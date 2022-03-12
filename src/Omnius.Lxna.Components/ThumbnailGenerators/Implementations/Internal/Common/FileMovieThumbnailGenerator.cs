using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using ImageMagick;
using Omnius.Core;
using Omnius.Core.RocketPack;
using Omnius.Core.Serialization;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Models;
using Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common;

public sealed class FileMovieThumbnailGenerator : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;
    private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;

    private static readonly HashSet<string> _extensionSet = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg", ".flv" };

    private static readonly Base16 _base16 = new Base16(ConvertStringCase.Lower);

    public static async ValueTask<FileMovieThumbnailGenerator> CreateAsync(IBytesPool bytesPool, string configDirectoryPath, int concurrency, CancellationToken cancellationToken = default)
    {
        var result = new FileMovieThumbnailGenerator(bytesPool, configDirectoryPath, concurrency);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private FileMovieThumbnailGenerator(IBytesPool bytesPool, string configDirectoryPath, int concurrency)
    {
        _bytesPool = bytesPool;
        _configDirectoryPath = configDirectoryPath;
        _concurrency = concurrency;
        _thumbnailGeneratorRepository = new ThumbnailGeneratorRepository(Path.Combine(_configDirectoryPath, "thumbnails.db"), _bytesPool);
    }

    internal async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
        await _thumbnailGeneratorRepository.MigrateAsync(cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _thumbnailGeneratorRepository.Dispose();
    }

    public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        // Cache
        var result = await this.GetThumbnailFromCacheAsync(file, options, cancellationToken).ConfigureAwait(false);
        if (result.Status == ThumbnailGeneratorGetThumbnailResultStatus.Succeeded) return result;
        if (cacheOnly) return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);

        return result = await this.GetMovieThumbnailAsync(file, options, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailFromCacheAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        var cache = await _thumbnailGeneratorRepository.ThumbnailCaches.FindOneAsync(file.LogicalPath, options.Width, options.Height, options.ResizeType, options.FormatType);
        if (cache is null) return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);

        var fileLength = await file.GetLengthAsync(cancellationToken);
        var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken);

        if ((ulong)fileLength != cache.FileMeta.Length && Timestamp.FromDateTime(fileLastWriteTime) != cache.FileMeta.LastWriteTime)
        {
            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);
        }

        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Succeeded, cache.Contents);
    }

    private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetMovieThumbnailAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        var ext = file.LogicalPath.GetExtension().ToLower();
        if (!_extensionSet.Contains(ext)) return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);

        try
        {
            var fileLength = await file.GetLengthAsync(cancellationToken);
            var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken);

            var images = await this.GetMovieImagesAsync(file, options.MinInterval, options.MaxImageCount, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken).ConfigureAwait(false);

            var fileMeta = new FileMeta(file.LogicalPath, (ulong)fileLength, Timestamp.FromDateTime(fileLastWriteTime));
            var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
            var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
            var cache = new ThumbnailCache(fileMeta, thumbnailMeta, contents);

            await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Succeeded, cache.Contents);
        }
        catch (NotSupportedException e)
        {
            _logger.Warn(e);
        }
        catch (OperationCanceledException e)
        {
            _logger.Debug(e);
        }
        catch (Exception e)
        {
            _logger.Error(e);
            throw;
        }

        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);
    }

    private async ValueTask<IMemoryOwner<byte>[]> GetMovieImagesAsync(IFile file, TimeSpan minInterval, int maxImageCount, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        var physicalFilePath = await file.GetPhysicalPathAsync(cancellationToken);

        var duration = await GetMovieDurationAsync(physicalFilePath, cancellationToken).ConfigureAwait(false);
        int intervalSeconds = (int)Math.Max(minInterval.TotalSeconds, duration.TotalSeconds / maxImageCount);
        int imageCount = (int)(duration.TotalSeconds / intervalSeconds);

        var resultMap = new ConcurrentDictionary<int, IMemoryOwner<byte>>();

        await Enumerable.Range(1, imageCount)
            .Select(x => x * intervalSeconds)
            .Where(seekSec => (duration.TotalSeconds - seekSec) > 1) // 残り1秒以下の場合は除外
            .ForEachAsync(
                async seekSec =>
                {
                    var ret = await this.GetMovieImagesAsync(physicalFilePath, seekSec, width, height, resizeType, formatType, cancellationToken);
                    resultMap.TryAdd(ret.SeekSec, ret.MemoryOwner);
                }, (int)_concurrency, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (!resultMap.IsEmpty)
        {
            var tempList = resultMap.ToList();
            tempList.Sort((x, y) => x.Key.CompareTo(y.Key));

            return tempList.Select(n => n.Value).ToArray();
        }

        var ret = await this.GetMovieImageAsync(physicalFilePath, width, height, resizeType, formatType, cancellationToken);
        return new[] { ret };
    }

    private static async ValueTask<TimeSpan> GetMovieDurationAsync(string path, CancellationToken cancellationToken = default)
    {
        var arguments = $"-v error -select_streams v:0 -show_entries stream=duration -sexagesimal -of default=noprint_wrappers=1:nokey=1 \"{path}\"";

        using var process = Process.Start(new ProcessStartInfo("ffprobe", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
        });

        if (process is null) throw new NotSupportedException();

        using var baseStream = process.StandardOutput.BaseStream;
        using var reader = new StreamReader(baseStream);
        var line = await reader.ReadLineAsync().ConfigureAwait(false);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (line == null || !TimeSpan.TryParse(line.Trim(), out var result)) throw new NotSupportedException();

        return result;
    }

    private async ValueTask<(int SeekSec, IMemoryOwner<byte> MemoryOwner)> GetMovieImagesAsync(string path, int seekSec, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = $"-loglevel error -ss {seekSec} -i \"{path}\" -vframes 1 -f image2 pipe:1";

            using var process = Process.Start(new ProcessStartInfo("ffmpeg", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            });

            using var baseStream = process!.StandardOutput.BaseStream;

            using var inStream = new RecyclableMemoryStream(_bytesPool);
            using var outStream = new RecyclableMemoryStream(_bytesPool);

            await baseStream.CopyToAsync(inStream, cancellationToken);
            inStream.Seek(0, SeekOrigin.Begin);
            await process.WaitForExitAsync(cancellationToken);

            this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

            return (seekSec, outStream.ToMemoryOwner());
        }
        catch (Exception e)
        {
            _logger.Warn(e);
            throw;
        }
    }

    private async ValueTask<IMemoryOwner<byte>> GetMovieImageAsync(string path, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        try
        {
            var arguments = $"-loglevel error -i \"{path}\" -vf thumbnail=30 -frames:v 1 -f image2 pipe:1";

            using var process = Process.Start(new ProcessStartInfo("ffmpeg", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            });

            using var baseStream = process!.StandardOutput.BaseStream;

            using var inStream = new RecyclableMemoryStream(_bytesPool);
            using var outStream = new RecyclableMemoryStream(_bytesPool);

            await baseStream.CopyToAsync(inStream, cancellationToken);
            inStream.Seek(0, SeekOrigin.Begin);
            await process.WaitForExitAsync(cancellationToken);

            this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

            return outStream.ToMemoryOwner();
        }
        catch (Exception e)
        {
            _logger.Warn(e);
            throw;
        }
    }

    private void ConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
    {
        try
        {
            this.InternalImageSharpConvertImage(inStream, outStream, width, height, resizeType, formatType);
        }
        catch (SixLabors.ImageSharp.ImageFormatException)
        {
            inStream.Seek(0, SeekOrigin.Begin);

            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                this.InternalMagickImageConvertImage(inStream, bitmapStream);
                bitmapStream.Seek(0, SeekOrigin.Begin);

                this.InternalImageSharpConvertImage(bitmapStream, outStream, width, height, resizeType, formatType);
            }
        }
    }

    private void InternalMagickImageConvertImage(Stream inStream, Stream outStream)
    {
        try
        {
            using var magickImage = new MagickImage(inStream, MagickFormat.Unknown);
            magickImage.Write(outStream, MagickFormat.Png32);
        }
        catch (Exception e)
        {
            throw new NotSupportedException(e.GetType().ToString(), e);
        }
    }

    private void InternalImageSharpConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
    {
        using var image = SixLabors.ImageSharp.Image.Load(inStream);
        image.Mutate(x =>
        {
            var resizeOptions = new ResizeOptions
            {
                Position = AnchorPositionMode.Center,
                Size = new SixLabors.ImageSharp.Size(width, height),
                Mode = resizeType switch
                {
                    ThumbnailResizeType.Pad => ResizeMode.Pad,
                    ThumbnailResizeType.Crop => ResizeMode.Crop,
                    _ => throw new NotSupportedException(),
                },
            };

            x.Resize(resizeOptions);
        });

        if (formatType == ThumbnailFormatType.Png)
        {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            image.Save(outStream, encoder);
            return;
        }

        throw new NotSupportedException();
    }
}