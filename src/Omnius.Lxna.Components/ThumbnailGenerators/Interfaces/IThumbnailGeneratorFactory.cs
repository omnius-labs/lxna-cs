namespace Omnius.Lxna.Components.ThumbnailGenerators;

public interface IThumbnailGeneratorFactory
{
    ValueTask<IThumbnailGenerator> CreateAsync(CancellationToken cancellationToken = default);
}
