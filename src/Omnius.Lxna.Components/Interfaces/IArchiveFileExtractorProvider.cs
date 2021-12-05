using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components;

public interface IArchiveFileExtractorProvider : IDisposable
{
    ValueTask<IArchiveFileExtractor> CreateAsync(NestedPath path, CancellationToken cancellationToken = default);
    ValueTask ShrinkAsync(CancellationToken cancellationToken = default);
}
