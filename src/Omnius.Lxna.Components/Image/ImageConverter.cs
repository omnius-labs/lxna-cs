using ImageMagick;
using Omnius.Core;
using Omnius.Core.Streams;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace Omnius.Lxna.Components.Image;

public sealed class ImageConverter
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;

    public static async ValueTask<ImageConverter> CreateAsync(IBytesPool bytesPool, CancellationToken cancellationToken = default)
    {
        var result = new ImageConverter(bytesPool);
        await result.InitAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    private ImageConverter(IBytesPool bytesPool)
    {
        _bytesPool = bytesPool;
    }

    internal async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
    }

    public async ValueTask ConvertAsync(Stream inStream, Stream outStream, ImageFormatType formatType,
        string? inputFileExtension = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.InternalImageSharpConvertAsync(inStream, outStream, formatType, cancellationToken).ConfigureAwait(false);
        }
        catch (SixLabors.ImageSharp.ImageFormatException)
        {
            inStream.Seek(0, SeekOrigin.Begin);

            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                await this.InternalMagickImageConvertAsync(inStream, bitmapStream, inputFileExtension, cancellationToken).ConfigureAwait(false);
                bitmapStream.Seek(0, SeekOrigin.Begin);

                await this.InternalImageSharpConvertAsync(bitmapStream, outStream, formatType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask ConvertAsync(Stream inStream, Stream outStream, ImageResizeType resizeType, int width, int height, ImageFormatType formatType,
        string? inputFileExtension = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.InternalImageSharpConvertAsync(inStream, outStream, resizeType, width, height, formatType, cancellationToken).ConfigureAwait(false);
        }
        catch (SixLabors.ImageSharp.ImageFormatException)
        {
            inStream.Seek(0, SeekOrigin.Begin);

            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                await this.InternalMagickImageConvertAsync(inStream, bitmapStream, inputFileExtension, cancellationToken).ConfigureAwait(false);
                bitmapStream.Seek(0, SeekOrigin.Begin);

                await this.InternalImageSharpConvertAsync(bitmapStream, outStream, resizeType, width, height, formatType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask InternalMagickImageConvertAsync(Stream inStream, Stream outStream, string? inputFileExtension = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var magickFormat = MagickFormat.Unknown;

            if (inputFileExtension is not null)
            {
                if (inputFileExtension == ".svg") magickFormat = MagickFormat.Svg;
            }

            using var magickImage = new MagickImage(inStream, magickFormat);
            await magickImage.WriteAsync(outStream, MagickFormat.Png, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new NotSupportedException(e.GetType().ToString(), e);
        }
    }

    private async ValueTask InternalImageSharpConvertAsync(Stream inStream, Stream outStream, ImageFormatType formatType, CancellationToken cancellationToken = default)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(inStream).ConfigureAwait(false);

        if (formatType == ImageFormatType.Png)
        {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder()
            {
                CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.Level1
            };
            await image.SaveAsync(outStream, encoder, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new NotSupportedException();
    }

    private async ValueTask InternalImageSharpConvertAsync(Stream inStream, Stream outStream, ImageResizeType resizeType, int width, int height, ImageFormatType formatType, CancellationToken cancellationToken = default)
    {
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(inStream).ConfigureAwait(false);

        image.Mutate(x =>
        {
            var resizeOptions = new ResizeOptions
            {
                Mode = resizeType switch
                {
                    ImageResizeType.Pad => ResizeMode.Pad,
                    ImageResizeType.Crop => ResizeMode.Crop,
                    ImageResizeType.Max => ResizeMode.Max,
                    ImageResizeType.Min => ResizeMode.Min,
                    _ => throw new NotSupportedException(),
                },
                Position = AnchorPositionMode.Center,
                Sampler = LanczosResampler.Lanczos3,
                Size = new SixLabors.ImageSharp.Size(width, height),
                PadColor = Color.Transparent,
            };
            x.BackgroundColor(Color.Transparent).Resize(resizeOptions);
        });

        if (formatType == ImageFormatType.Png)
        {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder()
            {
                CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.Level1
            };
            await image.SaveAsync(outStream, encoder, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new NotSupportedException();
    }
}
