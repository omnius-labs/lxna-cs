namespace Omnius.Lxna.Components.Storage;

public interface IFile : IDisposable
{
    string Name { get; }
    string Extension { get; }
    NestedPath LogicalPath { get; }
    FileAttributes Attributes { get; }
    bool IsReadOnly { get; }
    bool IsArchive { get; }
    bool Exists { get; }

    ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default);
    ValueTask<DateTime> GetCreationTimeAsync(CancellationToken cancellationToken = default);
    ValueTask<DateTime> GetLastAccessTimeAsync(CancellationToken cancellationToken = default);
    ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default);
    ValueTask<long> GetLengthAsync(CancellationToken cancellationToken = default);

    ValueTask<Stream> GetStreamAsync(CancellationToken cancellationToken = default);
    ValueTask<IDirectory?> TryConvertToDirectoryAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> TryMoveToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default);
    ValueTask<bool> TryCopyToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default);
    ValueTask<bool> TryDeleteAsync(bool recursive, CancellationToken cancellationToken = default);
}

[Flags]
public enum FileAttributes
{
    Unknown = 0,
    Normal = 0x01,
    Archive = 0x02,
}
