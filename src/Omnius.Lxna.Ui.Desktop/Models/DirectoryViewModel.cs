using Omnius.Core.Avalonia;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Ui.Desktop.Models;

public sealed class DirectoryViewModel : BindableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IFileSystem _fileSystem;

    public DirectoryViewModel(NestedPath path, IFileSystem fileSystem)
    {
        this.Path = path;
        _fileSystem = fileSystem;

        if (path == NestedPath.Empty) return;

        this.Children = new[] { new DirectoryViewModel(NestedPath.Empty, _fileSystem) };
    }

    private NestedPath _path = NestedPath.Empty;

    public NestedPath Path
    {
        get => _path;
        private set
        {
            var name = string.Empty;
            if (value != NestedPath.Empty) name = value.GetName();

            this.SetProperty(ref _path, value);
            this.Name = name;
        }
    }

    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        private set => this.SetProperty(ref _name, value);
    }

    private bool _isExpanded = false;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            this.SetProperty(ref _isExpanded, value);
            this.RefreshChildrenAsync();
        }
    }

    private IList<DirectoryViewModel> _children = Array.Empty<DirectoryViewModel>();

    public IList<DirectoryViewModel> Children
    {
        get => _children;
        set => this.SetProperty(ref _children, value);
    }

    // FIXME
    public async void RefreshChildrenAsync()
    {
        var children = new List<DirectoryViewModel>();

        foreach (var directoryPath in await _fileSystem.FindDirectoriesAsync(this.Path))
        {
            children.Add(new DirectoryViewModel(directoryPath, _fileSystem));
        }

        foreach (var archiveFilePath in await _fileSystem.FindArchiveFilesAsync(this.Path))
        {
            children.Add(new DirectoryViewModel(archiveFilePath, _fileSystem));
        }

        this.Children = children;
    }
}
