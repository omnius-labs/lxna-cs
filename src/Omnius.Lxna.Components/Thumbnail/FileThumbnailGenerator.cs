using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Omnius.Core;
using Omnius.Core.Collections;
using Omnius.Core.RocketPack;
using Omnius.Core.Serialization;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail.Internal;
using Omnius.Lxna.Components.Thumbnail.Internal.Repositories;

namespace Omnius.Lxna.Components.Thumbnail;

public record FileThumbnailGeneratorOptions
{
    public required string StateDirectoryPath { get; init; }
    public required int Concurrency { get; init; }
}

public sealed class FileThumbnailGenerator : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _stateDirectoryPath;
    private readonly int _concurrency;
    private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;
    private readonly ImageConverter _imageConverter;

    private static readonly HashSet<string> _movieExtensionSet = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg", ".flv" };

    private static readonly Base16 _base16 = new Base16();

    public static async ValueTask<FileThumbnailGenerator> CreateAsync(ImageConverter imageConverter, IBytesPool bytesPool, FileThumbnailGeneratorOptions options, CancellationToken cancellationToken = default)
    {
        var result = new FileThumbnailGenerator(imageConverter, bytesPool, options);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private FileThumbnailGenerator(ImageConverter imageConverter, IBytesPool bytesPool, FileThumbnailGeneratorOptions options)
    {
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
        _stateDirectoryPath = options.StateDirectoryPath;
        _concurrency = options.Concurrency;
        _thumbnailGeneratorRepository = new ThumbnailGeneratorRepository(Path.Combine(_stateDirectoryPath, "ThumbnailGenerators.db"), _bytesPool);

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
        var cache = await _thumbnailGeneratorRepository.ThumbnailCaches.FindOneAsync(file.LogicalPath, options.Width, options.Height, options.ResizeType, options.FormatType).ConfigureAwait(false);
        if (cache is null) return new FileThumbnailResult(FileThumbnailResultStatus.Failed);

        var fileLength = await file.GetLengthAsync(cancellationToken).ConfigureAwait(false);
        var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken).ConfigureAwait(false);

        if ((ulong)fileLength != cache.FileMeta.Length && Timestamp64.FromDateTime(fileLastWriteTime) != cache.FileMeta.LastWriteTime)
        {
            return new FileThumbnailResult(FileThumbnailResultStatus.Failed);
        }

        return new FileThumbnailResult(FileThumbnailResultStatus.Succeeded, cache.Contents);
    }

    private async ValueTask<FileThumbnailResult> GetPictureThumbnailAsync(IFile file, FileThumbnailOptions options, CancellationToken cancellationToken = default)
    {

        try
        {
            var fileLength = await file.GetLengthAsync(cancellationToken).ConfigureAwait(false);
            var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken).ConfigureAwait(false);
            var extension = file.LogicalPath.GetExtension().ToLower(CultureInfo.InvariantCulture);

            using (var inStream = await file.GetStreamAsync(cancellationToken).ConfigureAwait(false))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                await this.ConvertImageAsync(inStream, extension, outStream, options.Width, options.Height, options.ResizeType, options.FormatType).ConfigureAwait(false);
                outStream.Seek(0, SeekOrigin.Begin);

                var image = outStream.ToMemoryOwner();

                var fileMeta = new FileMeta(file.LogicalPath, (ulong)fileLength, Timestamp64.FromDateTime(fileLastWriteTime));
                var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                var content = new ThumbnailContent(image);
                var cache = new ThumbnailCache(fileMeta, thumbnailMeta, [content]);

                await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache).ConfigureAwait(false);

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
        var extension = file.LogicalPath.GetExtension().ToLower(CultureInfo.InvariantCulture);
        if (!_movieExtensionSet.Contains(extension)) return new FileThumbnailResult(FileThumbnailResultStatus.Failed);

        try
        {
            var fileLength = await file.GetLengthAsync(cancellationToken).ConfigureAwait(false);
            var fileLastWriteTime = await file.GetLastWriteTimeAsync(cancellationToken).ConfigureAwait(false);

            var images = await this.GetMovieImagesAsync(file, options.MinInterval, options.MaxImageCount, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken).ConfigureAwait(false);

            var fileMeta = new FileMeta(file.LogicalPath, (ulong)fileLength, Timestamp64.FromDateTime(fileLastWriteTime));
            var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
            var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
            var cache = new ThumbnailCache(fileMeta, thumbnailMeta, contents);

            await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache).ConfigureAwait(false);

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
        var physicalFilePath = await file.GetPhysicalPathAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var duration = await GetMovieDurationAsync(physicalFilePath, cancellationToken).ConfigureAwait(false);
            int intervalSeconds = (int)Math.Max(minInterval.TotalSeconds, duration.TotalSeconds / maxImageCount);
            int imageCount = (int)(duration.TotalSeconds / intervalSeconds);

            var resultMap = new ConcurrentDictionary<int, IMemoryOwner<byte>>();

            var seekSecs = Enumerable.Range(1, imageCount)
                .Select(x => x * intervalSeconds)
                .Where(seekSec => (duration.TotalSeconds - seekSec) > 1) // 残り1秒以下の場合は除外
                .ToList();
            var parallelOptions = new ParallelOptions() { CancellationToken = cancellationToken, MaxDegreeOfParallelism = _concurrency };

            await Parallel.ForEachAsync(seekSecs, parallelOptions, async (seekSec, _) =>
            {
                var ret = await this.GetMovieImagesAsync(physicalFilePath, seekSec, width, height, resizeType, formatType, cancellationToken).ConfigureAwait(false);
                resultMap.TryAdd(ret.SeekSec, ret.MemoryOwner);
            }).ConfigureAwait(false);

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

        var ret = await this.GetMovieImageAsync(physicalFilePath, width, height, resizeType, formatType, cancellationToken).ConfigureAwait(false);
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
        var arguments = $"-loglevel error -ss {seekSec} -i \"{path}\" -vframes 1 -c:v png -f image2 pipe:1";

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

        await baseStream.CopyToAsync(inStream, cancellationToken).ConfigureAwait(false);
        inStream.Seek(0, SeekOrigin.Begin);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        await this.ConvertImageAsync(inStream, ".png", outStream, width, height, resizeType, formatType).ConfigureAwait(false);

        return (seekSec, outStream.ToMemoryOwner());
    }

    private async ValueTask<IMemoryOwner<byte>> GetMovieImageAsync(string path, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        var arguments = $"-loglevel error -i \"{path}\" -vf thumbnail=30 -frames:v 1 -c:v png -f image2 pipe:1";

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

        await baseStream.CopyToAsync(inStream, cancellationToken).ConfigureAwait(false);
        inStream.Seek(0, SeekOrigin.Begin);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        await this.ConvertImageAsync(inStream, ".png", outStream, width, height, resizeType, formatType).ConfigureAwait(false);

        return outStream.ToMemoryOwner();
    }

    private async ValueTask ConvertImageAsync(Stream inStream, string extension, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
    {
        var imageResizeType = resizeType switch
        {
            ThumbnailResizeType.Pad => ImageResizeType.Pad,
            ThumbnailResizeType.Crop => ImageResizeType.Crop,
            _ => throw new FormatException("unknown resize type")
        };
        var imageFormatType = formatType switch
        {
            ThumbnailFormatType.Png => ImageFormatType.Png,
            _ => throw new FormatException("unknown format type")
        };
        await _imageConverter.ConvertAsync(inStream, outStream, width, height, imageResizeType, imageFormatType, cancellationToken);
    }
}

public record FileThumbnailOptions
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ThumbnailFormatType FormatType { get; init; }
    public required ThumbnailResizeType ResizeType { get; init; }
    public required TimeSpan MinInterval { get; init; }
    public required int MaxImageCount { get; init; }
}

public readonly struct FileThumbnailResult
{
    public FileThumbnailResult(FileThumbnailResultStatus status, IEnumerable<ThumbnailContent>? contents = null)
    {
        this.Status = status;
        this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
    }

    public FileThumbnailResultStatus Status { get; }
    public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
}

public enum FileThumbnailResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
