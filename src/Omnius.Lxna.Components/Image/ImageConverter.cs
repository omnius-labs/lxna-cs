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
        await result.InitAsync(cancellationToken);

        return result;
    }

    private ImageConverter(IBytesPool bytesPool)
    {
        _bytesPool = bytesPool;
    }

    internal async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
    }

    public async ValueTask ConvertAsync(Stream inStream, Stream outStream, ImageFormatType formatType, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.InternalImageSharpConvertAsync(inStream, outStream, formatType, cancellationToken);
        }
        catch (SixLabors.ImageSharp.ImageFormatException)
        {
            inStream.Seek(0, SeekOrigin.Begin);

            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                await this.InternalMagickImageConvertAsync(inStream, bitmapStream, cancellationToken);
                bitmapStream.Seek(0, SeekOrigin.Begin);

                await this.InternalImageSharpConvertAsync(bitmapStream, outStream, formatType, cancellationToken);
            }
        }
    }

    public async ValueTask ConvertAsync(Stream inStream, Stream outStream, int width, int height, ImageResizeType resizeType, ImageFormatType formatType, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.InternalImageSharpConvertAsync(inStream, outStream, width, height, resizeType, formatType, cancellationToken);
        }
        catch (SixLabors.ImageSharp.ImageFormatException)
        {
            inStream.Seek(0, SeekOrigin.Begin);

            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                await this.InternalMagickImageConvertAsync(inStream, bitmapStream, cancellationToken);
                bitmapStream.Seek(0, SeekOrigin.Begin);

                await this.InternalImageSharpConvertAsync(bitmapStream, outStream, width, height, resizeType, formatType, cancellationToken);
            }
        }
    }

    private async ValueTask InternalMagickImageConvertAsync(Stream inStream, Stream outStream, CancellationToken cancellationToken = default)
    {
        try
        {
            var magickFormat = MagickFormat.Unknown;

            if (inStream is FileStream fileStream)
            {
                var ext = Path.GetExtension(fileStream.Name);
                if (ext == ".svg") magickFormat = MagickFormat.Svg;
            }

            using var magickImage = new MagickImage(inStream, magickFormat);
            await magickImage.WriteAsync(outStream, MagickFormat.Png, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new NotSupportedException(e.GetType().ToString(), e);
        }
    }

    private async ValueTask InternalImageSharpConvertAsync(Stream inStream, Stream outStream, int width, int height, ImageResizeType resizeType, ImageFormatType formatType, CancellationToken cancellationToken = default)
    {
        using var image = SixLabors.ImageSharp.Image.Load(inStream);

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
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            await image.SaveAsync(outStream, encoder, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new NotSupportedException();
    }

    private async ValueTask InternalImageSharpConvertAsync(Stream inStream, Stream outStream, ImageFormatType formatType, CancellationToken cancellationToken = default)
    {
        using var image = SixLabors.ImageSharp.Image.Load(inStream);

        if (formatType == ImageFormatType.Png)
        {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            await image.SaveAsync(outStream, encoder, cancellationToken).ConfigureAwait(false);
            return;
        }

        throw new NotSupportedException();
    }
}
