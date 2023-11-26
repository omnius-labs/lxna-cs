using Omnius.Core;
using Omnius.Lxna.Components.IconGenerators.Models;
using Omnius.Lxna.Components.Storages;

namespace Omnius.Lxna.Components.IconGenerators;

public interface IDirectoryIconGeneratorFactory
{
    ValueTask<IDirectoryIconGenerator> CreateAsync(IBytesPool bytesPool, DirectoryIconGeneratorOptions options, CancellationToken cancellationToken = default);
}

public interface IDirectoryIconGenerator : IAsyncDisposable
{
    ValueTask<DirectoryIconResult> GenerateAsync(IDirectory directory, DirectoryIconOptions options, bool isCacheOnly, CancellationToken cancellationToken = default);
}
