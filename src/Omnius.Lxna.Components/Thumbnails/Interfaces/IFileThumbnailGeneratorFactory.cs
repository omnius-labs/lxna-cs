namespace Omnius.Lxna.Components.Thumbnails;

public interface IFileThumbnailGeneratorFactory
{
    ValueTask<IFileThumbnailGenerator> CreateAsync(CancellationToken cancellationToken = default);
}
