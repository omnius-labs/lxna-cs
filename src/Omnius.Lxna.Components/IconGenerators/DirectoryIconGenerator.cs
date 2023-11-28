using ImageMagick;
using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.IconGenerators.Models;
using Omnius.Lxna.Components.Storages;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components.IconGenerators;

public record DirectoryIconGeneratorOptions
{
    public required string ConfigDirectoryPath { get; init; }
    public required int Concurrency { get; init; }
}

public sealed class DirectoryIconGenerator : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IBytesPool _bytesPool;
    private readonly string _configDirectoryPath;
    private readonly int _concurrency;

    public static async ValueTask<DirectoryIconGenerator> CreateAsync(IBytesPool bytesPool, DirectoryIconGeneratorOptions options, CancellationToken cancellationToken = default)
    {
        var result = new DirectoryIconGenerator(bytesPool, options);
        await result.InitAsync(cancellationToken);
        return result;
    }

    internal DirectoryIconGenerator(IBytesPool bytesPool, DirectoryIconGeneratorOptions options)
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

    public async ValueTask<DirectoryIconResult> GenerateAsync(IDirectory directory, DirectoryIconOptions options, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        try
        {
            var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location);
            basePath ??= Directory.GetCurrentDirectory();

            using (var inStream = new FileStream(Path.Combine(basePath, "Assets/directory.svg"), FileMode.Open))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                this.ConvertImage(inStream, outStream, options.Width, options.Height, options.FormatType);
                outStream.Seek(0, SeekOrigin.Begin);

                var image = outStream.ToMemoryOwner();
                var content = new IconContent(image);

                return new DirectoryIconResult
                {
                    Status = DirectoryIconResultStatus.Succeeded,
                    Content = content
                };
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

        return new DirectoryIconResult { Status = DirectoryIconResultStatus.Failed };
    }

    private void ConvertImage(Stream inStream, Stream outStream, int width, int height, IconFormatType formatType)
    {
        using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
        {
            this.InternalMagickImageConvertImage(inStream, bitmapStream);
            bitmapStream.Seek(0, SeekOrigin.Begin);

            this.InternalImageSharpConvertImage(bitmapStream, outStream, width, height, formatType);
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

    private void InternalImageSharpConvertImage(Stream inStream, Stream outStream, int width, int height, IconFormatType formatType)
    {
        using var image = SixLabors.ImageSharp.Image.Load(inStream);
        image.Mutate(x =>
        {
            var resizeOptions = new ResizeOptions
            {
                Position = AnchorPositionMode.Center,
                Size = new SixLabors.ImageSharp.Size(width, height),
                Mode = ResizeMode.Stretch,
            };

            x.Resize(resizeOptions);
        });

        if (formatType == IconFormatType.Png)
        {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            image.Save(outStream, encoder);
            return;
        }

        throw new NotSupportedException();
    }
}
