namespace Omnius.Lxna.Components.ThumbnailGenerators;

public interface IFileThumbnailGeneratorFactory
{
    ValueTask<IFileThumbnailGenerator> CreateAsync(CancellationToken cancellationToken = default);
}
