using Omnius.Lxna.Components.Storage.Models;

namespace Omnius.Lxna.Components.Storage;

public interface IFile : IDisposable
{
    public string Name { get; }

    public NestedPath LogicalPath { get; }

    public FileAttributes Attributes { get; }

    ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default);

    ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default);

    ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken = default);

    ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default);

    ValueTask<IDirectory?> TryConvertToDirectory(CancellationToken cancellationToken = default);
}

[Flags]
public enum FileAttributes
{
    Unknown = 0,
    Normal = 0x01,
    Archive = 0x02,
}
