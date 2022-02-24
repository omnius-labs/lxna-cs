using System.Runtime.CompilerServices;
using Omnius.Core;
using Omnius.Lxna.Components.Storage.Models;
using Omnius.Lxna.Components.Storage.Windows.Internal;
using Omnius.Lxna.Components.Storage.Windows.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage.Windows;

internal sealed class LocalDirectory : IDirectory
{
    private readonly string _physicalPath;
    private readonly string _tempPath;
    private readonly IBytesPool _bytesPool;

    public LocalDirectory(string physicalPath, string tempPath, IBytesPool bytesPool)
    {
        _physicalPath = physicalPath;
        _tempPath = tempPath;
        _bytesPool = bytesPool;

        this.LogicalPath = new NestedPath(physicalPath);
        this.Name = this.LogicalPath.GetName();
    }

    public string Name { get; }

    public NestedPath LogicalPath { get; }


    public void Dispose()
    {
    }

    public async ValueTask<bool> ExistsFileAsync(string name, CancellationToken cancellationToken = default)
    {
        return File.Exists(System.IO.Path.Combine(_physicalPath, name));
    }

    public async ValueTask<bool> ExistsDirectoryAsync(string name, CancellationToken cancellationToken = default)
    {
        return Directory.Exists(System.IO.Path.Combine(_physicalPath, name));
    }

    public async IAsyncEnumerable<IDirectory> FindDirectoriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var path in Directory.EnumerateDirectories(_physicalPath, "*", new EnumerationOptions() { RecurseSubdirectories = false }))
        {
            yield return new LocalDirectory(PathHelper.Normalize(path), _tempPath, _bytesPool);
        }
    }

    public async IAsyncEnumerable<IFile> FindFilesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var path in Directory.EnumerateFiles(_physicalPath, "*", new EnumerationOptions() { RecurseSubdirectories = false }))
        {
            yield return new LocalFile(PathHelper.Normalize(path), _tempPath, _bytesPool);
        }
    }
}
