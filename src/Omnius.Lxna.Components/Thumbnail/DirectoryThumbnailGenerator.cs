using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Internal;

public sealed class DirectoryThumbnailGenerator
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly string _dirImagePath;
    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private int _cachedWidth = -1;
    private int _cachedHeight = -1;
    private byte[]? _cachedImage;

    private readonly AsyncLock _asyncLock = new();

    public DirectoryThumbnailGenerator(ImageConverter imageConverter, IBytesPool bytesPool)
    {
        var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location);
        basePath ??= Directory.GetCurrentDirectory();
        _dirImagePath = Path.Combine(basePath, "Assets/directory.svg");

        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
    }

    public async ValueTask<ThumbnailContent> GetThumbnailAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            if (_cachedImage is not null && width == _cachedWidth && height == _cachedHeight)
            {
                return new ThumbnailContent(new MemoryOwner<byte>(_cachedImage));
            }
        }

        using (var inStream = new FileStream(_dirImagePath, FileMode.Open))
        using (var outStream = new RecyclableMemoryStream(_bytesPool))
        {
            await _imageConverter.ConvertAsync(inStream, outStream, width, height, ImageResizeType.Crop, ImageFormatType.Png, cancellationToken);
            outStream.Seek(0, SeekOrigin.Begin);
            var image = await outStream.ToBytesAsync();

            using (await _asyncLock.LockAsync(cancellationToken))
            {
                _cachedWidth = width;
                _cachedHeight = height;
                _cachedImage = image;
            }

            return new ThumbnailContent(new MemoryOwner<byte>(image));
        }
    }
}
