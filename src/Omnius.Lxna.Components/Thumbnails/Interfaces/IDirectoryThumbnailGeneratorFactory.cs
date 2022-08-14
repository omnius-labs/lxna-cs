namespace Omnius.Lxna.Components.Thumbnails;

public interface IDirectoryThumbnailGeneratorFactory
{
    ValueTask<IDirectoryThumbnailGenerator> CreateAsync(CancellationToken cancellationToken = default);
}
