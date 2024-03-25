using System.Buffers;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;

namespace Omnius.Lxna.Ui.Desktop.Service.Thumbnail;

public sealed class Preview : BindableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IFile _file;
    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;
    private readonly RecyclableMemoryStream _imageStream;

    private IMemoryOwner<byte>? _resizedImageBytes;
    private int _resizedImageWidth;
    private int _resizedImageHeight;

    private readonly AsyncLock _asyncLock = new();

    public static async ValueTask<Preview> CreateAsync(IFile file, ImageConverter imageConverter, IBytesPool bytesPool, CancellationToken cancellationToken = default)
    {
        var preview = new Preview(file, imageConverter, bytesPool);
        await preview.InitAsync(cancellationToken);
        return preview;
    }

    internal Preview(IFile file, ImageConverter imageConverter, IBytesPool bytesPool)
    {
        _file = file;
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
        _imageStream = new RecyclableMemoryStream(_bytesPool);
    }

    private async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
        using (var inStream = await _file.GetStreamAsync(cancellationToken))
        {
            await _imageConverter.ConvertAsync(inStream, _imageStream, ImageFormatType.Png, cancellationToken);
            _imageStream.Seek(0, SeekOrigin.Begin);
        }
    }

    public void Dispose()
    {
        _imageStream?.Dispose();
    }

    public IFile File => _file;

    public async ValueTask<ReadOnlyMemory<byte>> GetImageBytesAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_resizedImageBytes != null && _resizedImageWidth == width && _resizedImageHeight == height)
            {
                return _resizedImageBytes.Memory;
            }

            _imageStream.Seek(0, SeekOrigin.Begin);

            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                await _imageConverter.ConvertAsync(_imageStream, outStream, ImageResizeType.Min, width, height, ImageFormatType.Png, cancellationToken);
                outStream.Seek(0, SeekOrigin.Begin);

                _resizedImageBytes = outStream.ToMemoryOwner();
                _resizedImageWidth = width;
                _resizedImageHeight = height;

                return _resizedImageBytes.Memory;
            }
        }
    }
}
