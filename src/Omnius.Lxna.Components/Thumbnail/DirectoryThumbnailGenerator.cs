using System.Collections.Immutable;
using Generator.Equals;
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

    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private ImmutableDictionary<string, byte[]> _originCache = ImmutableDictionary<string, byte[]>.Empty;
    private ImmutableDictionary<(string, DirectoryThumbnailOptions), byte[]> _resizedCache = ImmutableDictionary<(string, DirectoryThumbnailOptions), byte[]>.Empty;

    private readonly AsyncLock _asyncLock = new();

    public static async ValueTask<DirectoryThumbnailGenerator> CreateAsync(ImageConverter imageConverter, IBytesPool bytesPool, CancellationToken cancellationToken = default)
    {
        var result = new DirectoryThumbnailGenerator(imageConverter, bytesPool);
        await result.InitAsync(cancellationToken);

        return result;
    }

    internal DirectoryThumbnailGenerator(ImageConverter imageConverter, IBytesPool bytesPool)
    {
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
    }

    internal async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
        var basePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly()?.Location);
        basePath ??= Directory.GetCurrentDirectory();
        var dirPath = Path.Combine(basePath, "Assets/Directory");

        var builder = ImmutableDictionary.CreateBuilder<string, byte[]>();

        foreach (var filePath in Directory.EnumerateFiles(dirPath))
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            builder.Add(fileName, await this.LoadImageAsync(filePath, cancellationToken));
        }

        _originCache = builder.ToImmutable();
    }

    private async ValueTask<byte[]> LoadImageAsync(string path, CancellationToken cancellationToken = default)
    {
        using (var inStream = new FileStream(path, FileMode.Open))
        using (var outStream = new RecyclableMemoryStream(_bytesPool))
        {
            await _imageConverter.ConvertAsync(inStream, outStream, ImageFormatType.Png, null, cancellationToken);
            outStream.Seek(0, SeekOrigin.Begin);
            return await outStream.ToBytesAsync();
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
    }

    public async ValueTask<DirectoryThumbnailResult> GenerateAsync(IDirectory directory, DirectoryThumbnailOptions options, CancellationToken cancellationToken = default)
    {
        if (directory is null) throw new ArgumentNullException(nameof(directory));

        try
        {
            var type = this.DetectDirectoryType(directory);

            if (_resizedCache.TryGetValue((type, options), out var resizedImage))
            {
                return new DirectoryThumbnailResult(DirectoryThumbnailResultStatus.Succeeded, new ThumbnailContent(new MemoryOwner<byte>(resizedImage)));
            }

            using (var inStream = new MemoryStream(_originCache[type]))
            using (var outStream = new RecyclableMemoryStream(_bytesPool))
            {
                await _imageConverter.ConvertAsync(inStream, outStream, ImageFormatType.Png, options.ResizeType, options.Width, options.Height, null, cancellationToken);
                outStream.Seek(0, SeekOrigin.Begin);
                var image = outStream.ToMemoryOwner();

                _resizedCache = _resizedCache.SetItem((type, options), image.Memory.ToArray());

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

    private string DetectDirectoryType(IDirectory directory)
    {
        return "normal";
    }
}

[Equatable]
public partial record DirectoryThumbnailOptions
{
    public required ImageResizeType ResizeType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required ImageFormatType FormatType { get; init; }
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
