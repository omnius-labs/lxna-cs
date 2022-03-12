using ImageMagick;
using Omnius.Core;
using Omnius.Core.Serialization;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common;

public sealed class DirectoryThumbnailGenerator : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;
    private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;

    private static readonly HashSet<string> _extensionSet = new HashSet<string>() { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".heic" };

    private static readonly Base16 _base16 = new Base16(ConvertStringCase.Lower);

    public static async ValueTask<DirectoryThumbnailGenerator> CreateAsync(IBytesPool bytesPool, string configDirectoryPath, int concurrency, CancellationToken cancellationToken = default)
    {
        var result = new DirectoryThumbnailGenerator(bytesPool, configDirectoryPath, concurrency);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private DirectoryThumbnailGenerator(IBytesPool bytesPool, string configDirectoryPath, int concurrency)
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

    public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(IDirectory directory, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        return await this.GetPictureThumbnailAsync(directory, options, cancellationToken).ConfigureAwait(false);
    }
    private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetPictureThumbnailAsync(IDirectory directory, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location);
            basePath ??= Directory.GetCurrentDirectory();

            using (var inStream = new FileStream(Path.Combine(basePath, "Assets/directory.svg"), FileMode.Open))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                this.ConvertImage(inStream, outStream, options.Width, options.Height, options.ResizeType, options.FormatType);
                outStream.Seek(0, SeekOrigin.Begin);

                var image = outStream.ToMemoryOwner();
                var content = new ThumbnailContent(image);

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorGetThumbnailResultStatus.Succeeded, new[] { content });
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
        using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
        {
            this.InternalMagickImageConvertImage(inStream, bitmapStream);
            bitmapStream.Seek(0, SeekOrigin.Begin);

            this.InternalImageSharpConvertImage(bitmapStream, outStream, width, height, resizeType, formatType);
        }
    }

    private void InternalMagickImageConvertImage(Stream inStream, Stream outStream)
    {
        try
        {
            using var magickImage = new MagickImage(inStream, MagickFormat.Svg);
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
