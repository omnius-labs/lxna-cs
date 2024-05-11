namespace Lxna.Components.Storage;

public interface IDirectory : IDisposable
{
    string Name { get; }
    NestedPath LogicalPath { get; }
    DirectoryAttributes Attributes { get; }
    bool IsReadOnly { get; }
    bool Exists { get; }

    ValueTask<string> GetPhysicalPathAsync(CancellationToken cancellationToken = default);
    ValueTask<DateTime> GetCreationTimeAsync(CancellationToken cancellationToken = default);
    ValueTask<DateTime> GetLastAccessTimeAsync(CancellationToken cancellationToken = default);
    ValueTask<DateTime> GetLastWriteTimeAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> TryMoveToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default);
    ValueTask<bool> TryCopyToAsync(NestedPath path, bool overwrite, CancellationToken cancellationToken = default);
    ValueTask<bool> TryDeleteAsync(bool recursive, CancellationToken cancellationToken = default);

    ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<IFile>> FindFilesAsync(CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsFileAsync(string name, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsDirectoryAsync(string name, CancellationToken cancellationToken = default);
    ValueTask<bool> TryCreateDirectoryAsync(string name, CancellationToken cancellationToken = default);
    ValueTask<bool> TryCreateFileAsync(string name, CancellationToken cancellationToken = default);
    ValueTask<bool> TryDeleteDirectoryAsync(string name, bool recursive, CancellationToken cancellationToken = default);
    ValueTask<bool> TryDeleteFileAsync(string name, CancellationToken cancellationToken = default);
}

[Flags]
public enum DirectoryAttributes
{
    Unknown = 0,
    Normal = 0x01,
    Archive = 0x02,
}
