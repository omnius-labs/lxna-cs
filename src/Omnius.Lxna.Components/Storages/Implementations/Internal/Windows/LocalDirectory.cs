using Omnius.Core;
using Omnius.Lxna.Components.Storages.Internal.Windows.Helpers;
using Omnius.Lxna.Components.Storages.Models;

namespace Omnius.Lxna.Components.Storages.Internal.Windows;

internal sealed class LocalDirectory : IDirectory
{
    private readonly IBytesPool _bytesPool;
    private readonly string _physicalPath;
    private readonly string _tempPath;

    public LocalDirectory(IBytesPool bytesPool, string physicalPath, string tempPath)
    {
        _bytesPool = bytesPool;
        _physicalPath = physicalPath;
        _tempPath = tempPath;

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

    public async ValueTask<IEnumerable<IDirectory>> FindDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IDirectory>();

        foreach (var path in Directory.EnumerateDirectories(_physicalPath, "*", new EnumerationOptions() { RecurseSubdirectories = false }))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new LocalDirectory(_bytesPool, PathHelper.Normalize(path), _tempPath));
        }

        return results;
    }

    public async ValueTask<IEnumerable<IFile>> FindFilesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<IFile>();

        foreach (var path in Directory.EnumerateFiles(_physicalPath, "*", new EnumerationOptions() { RecurseSubdirectories = false }))
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(new LocalFile(_bytesPool, PathHelper.Normalize(path), _tempPath));
        }

        return results;
    }
}
