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

public sealed class FilePictureThumbnailGenerator : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;
    private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;

    private static readonly HashSet<string> _extensionSet = new HashSet<string>() { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".heic" };

    private static readonly Base16 _base16 = new Base16(ConvertStringCase.Lower);

    public static async ValueTask<FilePictureThumbnailGenerator> CreateAsync(IBytesPool bytesPool, string configDirectoryPath, int concurrency, CancellationToken cancellationToken = default)
    {
        var result = new FilePictureThumbnailGenerator(bytesPool, configDirectoryPath, concurrency);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private FilePictureThumbnailGenerator(IBytesPool bytesPool, string configDirectoryPath, int concurrency)
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

        return await this.GetPictureThumbnailAsync(file, options, cancellationToken).ConfigureAwait(false);
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

    private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetPictureThumbnailAsync(IFile file, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        var ext = file.LogicalPath.GetExtension().ToLower();
        if (!_extensionSet.Contains(ext)) return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);

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

                var fileMeta = new FileMeta(file.LogicalPath, (ulong)fileLength, Timestamp.FromDateTime(fileLastWriteTime));
                var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                var content = new ThumbnailContent(image);
                var cache = new ThumbnailCache(fileMeta, thumbnailMeta, new[] { content });

                await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Succeeded, cache.Contents);
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
            throw;
        }

        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Failed);
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
