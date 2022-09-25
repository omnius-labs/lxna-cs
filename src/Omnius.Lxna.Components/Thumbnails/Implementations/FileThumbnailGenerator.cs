using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using ImageMagick;
using Omnius.Core;
using Omnius.Core.RocketPack;
using Omnius.Core.Serialization;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.Thumbnails.Internal.Common.Models;
using Omnius.Lxna.Components.Thumbnails.Internal.Common.Repositories;
using Omnius.Lxna.Components.Thumbnails.Models;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components.Thumbnails;

public sealed class FileThumbnailGenerator : AsyncDisposableBase, IFileThumbnailGenerator
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;
    private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;

    private static readonly HashSet<string> _pictureExtensionSet = new HashSet<string>() { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".heic" };
    private static readonly HashSet<string> _movieExtensionSet = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg", ".flv" };

    private static readonly Base16 _base16 = new Base16(ConvertStringCase.Lower);

    public static async ValueTask<FileThumbnailGenerator> CreateAsync(IBytesPool bytesPool, FileThumbnailGeneratorOptions options, CancellationToken cancellationToken = default)
    {
        var result = new FileThumbnailGenerator(bytesPool, options);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private FileThumbnailGenerator(IBytesPool bytesPool, FileThumbnailGeneratorOptions options)
    {
        _bytesPool = bytesPool;
        _configDirectoryPath = options.ConfigDirectoryPath;
        _concurrency = options.Concurrency;
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

    public async ValueTask<FileThumbnailResult> GenerateAsync(IFile file, FileThumbnailOptions options, bool isCacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        var result = await this.GetThumbnailFromCacheAsync(file, options, cancellationToken).ConfigureAwait(false);
        if (isCacheOnly || result.Status == FileThumbnailResultStatus.Succeeded) return result;

        result = await this.GetMovieThumbnailAsync(file, options, cancellationToken).ConfigureAwait(false);
        if (result.Status == FileThumbnailResultStatus.Succeeded) return result;

        result = await this.GetPictureThumbnailAsync(file, options, cancellationToken).ConfigureAwait(false);
        if (result.Status == FileThumbnailResultStatus.Succeeded) return result;

        return new FileThumbnailResult(FileThumbnailResultStatus.Failed);
    }

    private async ValueTask<FileThumbnailResult> GetThumbnailFromCacheAsync(IFile file, FileThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        var cache = await _thumbnailGeneratorRepository.ThumbnailCaches.FindOneAsync(file.LogicalPath, options.Width, options.Height, options.ResizeType, options.FormatType);
        if (cache is null) return new FileThumbnailResult(FileThumbnailResultStatus.Failed);

        var fileLength = await file.GetLengthAsync(cancellationToken);
        var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken);

        if ((ulong)fileLength != cache.FileMeta.Length && Timestamp64.FromDateTime(fileLastWriteTime) != cache.FileMeta.LastWriteTime)
        {
            return new FileThumbnailResult(FileThumbnailResultStatus.Failed);
        }

        return new FileThumbnailResult(FileThumbnailResultStatus.Succeeded, cache.Contents);
    }

    private async ValueTask<FileThumbnailResult> GetPictureThumbnailAsync(IFile file, FileThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        var ext = file.LogicalPath.GetExtension().ToLower();
        if (!_movieExtensionSet.Contains(ext) && !_pictureExtensionSet.Contains(ext)) return new FileThumbnailResult(FileThumbnailResultStatus.Failed);

        try
        {
            var fileLength = await file.GetLengthAsync(cancellationToken);
            var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken);

            using (var inStream = await file.GetStreamAsync(cancellationToken))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                this.ConvertImage(inStream, outStream, options.Width, options.Height, options.ResizeType, options.FormatType);
                outStream.Seek(0, SeekOrigin.Begin);

                var image = outStream.ToMemoryOwner();

                var fileMeta = new FileMeta(file.LogicalPath, (ulong)fileLength, Timestamp64.FromDateTime(fileLastWriteTime));
                var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                var content = new ThumbnailContent(image);
                var cache = new ThumbnailCache(fileMeta, thumbnailMeta, new[] { content });

                await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);

                return new FileThumbnailResult(FileThumbnailResultStatus.Succeeded, cache.Contents);
            }
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
        }

        return new FileThumbnailResult(FileThumbnailResultStatus.Failed);
    }

    private async ValueTask<FileThumbnailResult> GetMovieThumbnailAsync(IFile file, FileThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        var ext = file.LogicalPath.GetExtension().ToLower();
        if (!_movieExtensionSet.Contains(ext)) return new FileThumbnailResult(FileThumbnailResultStatus.Failed);

        try
        {
            var fileLength = await file.GetLengthAsync(cancellationToken);
            var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken);

            var images = await this.GetMovieImagesAsync(file, options.MinInterval, options.MaxImageCount, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken).ConfigureAwait(false);

            var fileMeta = new FileMeta(file.LogicalPath, (ulong)fileLength, Timestamp64.FromDateTime(fileLastWriteTime));
            var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
            var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
            var cache = new ThumbnailCache(fileMeta, thumbnailMeta, contents);

            await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);

            return new FileThumbnailResult(FileThumbnailResultStatus.Succeeded, cache.Contents);
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
        }

        return new FileThumbnailResult(FileThumbnailResultStatus.Failed);
    }

    private async ValueTask<IMemoryOwner<byte>[]> GetMovieImagesAsync(IFile file, TimeSpan minInterval, int maxImageCount, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        var physicalFilePath = await file.GetPhysicalPathAsync(cancellationToken);

        try
        {
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
        }
        catch (Exception e)
        {
            _logger.Warn(e);
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
        var arguments = $"-loglevel error -ss {seekSec} -i \"{path}\" -vframes 1 -f image2 pipe:1";

        using var process = Process.Start(new ProcessStartInfo("ffmpeg", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
        });
        if (process is null) throw new NotSupportedException();

        using var baseStream = process.StandardOutput.BaseStream;

        using var inStream = new RecyclableMemoryStream(_bytesPool);
        using var outStream = new RecyclableMemoryStream(_bytesPool);

        await baseStream.CopyToAsync(inStream, cancellationToken);
        inStream.Seek(0, SeekOrigin.Begin);
        await process.WaitForExitAsync(cancellationToken);

        this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

        return (seekSec, outStream.ToMemoryOwner());
    }

    private async ValueTask<IMemoryOwner<byte>> GetMovieImageAsync(string path, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        var arguments = $"-loglevel error -i \"{path}\" -vf thumbnail=30 -frames:v 1 -f image2 pipe:1";

        using var process = Process.Start(new ProcessStartInfo("ffmpeg", arguments)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
        });
        if (process is null) throw new NotSupportedException();

        using var baseStream = process.StandardOutput.BaseStream;

        using var inStream = new RecyclableMemoryStream(_bytesPool);
        using var outStream = new RecyclableMemoryStream(_bytesPool);

        await baseStream.CopyToAsync(inStream, cancellationToken);
        inStream.Seek(0, SeekOrigin.Begin);
        await process.WaitForExitAsync(cancellationToken);

        this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

        return outStream.ToMemoryOwner();
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
