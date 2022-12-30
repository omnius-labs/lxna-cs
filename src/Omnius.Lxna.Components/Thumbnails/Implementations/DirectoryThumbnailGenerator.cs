using ImageMagick;
using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.Thumbnails.Models;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components.Thumbnails;

public sealed class DirectoryThumbnailGenerator : AsyncDisposableBase, IDirectoryThumbnailGenerator
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;

    public static async ValueTask<DirectoryThumbnailGenerator> CreateAsync(IBytesPool bytesPool, DirectoryThumbnailGeneratorOptions options, CancellationToken cancellationToken = default)
    {
        var result = new DirectoryThumbnailGenerator(bytesPool, options);
        await result.InitAsync(cancellationToken);

        return result;
    }

    private DirectoryThumbnailGenerator(IBytesPool bytesPool, DirectoryThumbnailGeneratorOptions options)
    {
        _bytesPool = bytesPool;
        _configDirectoryPath = options.ConfigDirectoryPath;
        _concurrency = options.Concurrency;
    }

    internal async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
    }

    protected override async ValueTask OnDisposeAsync()
    {
    }

    public async ValueTask<DirectoryThumbnailResult> GenerateAsync(IDirectory directory, DirectoryThumbnailOptions options, bool isCacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        try
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location);
            basePath ??= Directory.GetCurrentDirectory();

            using (var inStream = new FileStream(Path.Combine(basePath, "Assets/directory.svg"), FileMode.Open))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                this.ConvertImage(inStream, outStream, Math.Min(32, options.Width), Math.Min(32, options.Height), options.ResizeType, options.FormatType);
                outStream.Seek(0, SeekOrigin.Begin);

                var image = outStream.ToMemoryOwner();
                var content = new ThumbnailContent(image);

                return new DirectoryThumbnailResult(DirectoryThumbnailResultStatus.Succeeded, new[] { content });
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

        return new DirectoryThumbnailResult(DirectoryThumbnailResultStatus.Failed);
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
