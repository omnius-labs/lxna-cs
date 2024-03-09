using Omnius.Core;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;

namespace Omnius.Lxna.Components.Thumbnail;

public interface IDirectoryThumbnailGenerator : IAsyncDisposable
{
    ValueTask<DirectoryThumbnailResult> GenerateAsync(IDirectory directory, DirectoryThumbnailOptions options, CancellationToken cancellationToken = default);
}

public sealed class DirectoryThumbnailGenerator : AsyncDisposableBase, IDirectoryThumbnailGenerator
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly string _dirImagePath;
    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private readonly AsyncLock _asyncLock = new();

    public DirectoryThumbnailGenerator(ImageConverter imageConverter, IBytesPool bytesPool)
    {
        var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location);
        basePath ??= Directory.GetCurrentDirectory();
        _dirImagePath = Path.Combine(basePath, "Assets/directory.svg");

        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
    }

    protected override async ValueTask OnDisposeAsync()
    {
    }

    public async ValueTask<DirectoryThumbnailResult> GenerateAsync(IDirectory directory, DirectoryThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            using (var inStream = new FileStream(_dirImagePath, FileMode.Open))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                await _imageConverter.ConvertAsync(inStream, outStream, options.Width, options.Height, options.ResizeType, ImageFormatType.Png, cancellationToken);
                outStream.Seek(0, SeekOrigin.Begin);
                var image = outStream.ToMemoryOwner();

                return new DirectoryThumbnailResult(DirectoryThumbnailResultStatus.Succeeded, new ThumbnailContent(image));
            }
        }
        catch (NotSupportedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }

        return new DirectoryThumbnailResult(DirectoryThumbnailResultStatus.Failed);
    }
}

public record DirectoryThumbnailOptions
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ImageFormatType FormatType { get; init; }
    public required ImageResizeType ResizeType { get; init; }
}

public readonly struct DirectoryThumbnailResult
{
    public DirectoryThumbnailResult(DirectoryThumbnailResultStatus status, ThumbnailContent? content = null)
    {
        this.Status = status;
        this.Content = content;
    }

    public DirectoryThumbnailResultStatus Status { get; }
    public ThumbnailContent? Content { get; }
}

public enum DirectoryThumbnailResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
