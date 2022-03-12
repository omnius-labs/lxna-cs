using Omnius.Core;

namespace Omnius.Lxna.Components.ThumbnailGenerators;

public sealed partial class WindowsThumbnailGeneratorFactory : IThumbnailGeneratorFactory
{
    private readonly IBytesPool _bytesPool;
    private readonly WindowsThumbnailGeneratorFatcoryOptions _options;

    public WindowsThumbnailGeneratorFactory(IBytesPool bytesPool, WindowsThumbnailGeneratorFatcoryOptions options)
    {
        _bytesPool = bytesPool;
        _options = options;
    }

    public async ValueTask<IThumbnailGenerator> CreateAsync(CancellationToken cancellationToken = default)
    {
        return await WindowsThumbnailGenerator.CreateAsync(_bytesPool, _options.ConfigDirectoryPath, _options.Concurrency, cancellationToken);
    }
}
