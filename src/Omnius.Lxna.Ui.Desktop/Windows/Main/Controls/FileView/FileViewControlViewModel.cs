using System.Collections.ObjectModel;
using Avalonia.Controls;
using Omnius.Core;
using Reactive.Bindings;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Ui.Desktop.Internal;
using Omnius.Core.Pipelines;
using System.Reactive.Linq;
using Omnius.Lxna.Ui.Desktop.Internal.Models;
using Omnius.Lxna.Ui.Desktop.Interactors.Internal;
using Omnius.Core.Helpers;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class FileViewControlViewModelBase : AsyncDisposableBase
{
    public ReactivePropertySlim<GridLength>? TreeViewWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailHeight { get; protected set; }

    public ReadOnlyReactiveCollection<DirectoryTreeViewModel>? RootDirectories { get; protected set; }

    public ReactivePropertySlim<DirectoryTreeViewModel>? SelectedDirectory { get; protected set; }

    public ReadOnlyObservableCollection<IThumbnail<object>>? CurrentItems { get; protected set; }

    public abstract void SetViewCommands(IFileViewControlCommands commands);

    public abstract void NotifyThumbnailDoubleTapped(object item);

    public abstract void NotifyThumbnailPrepared(object item);

    public abstract void NotifyThumbnailClearing(object item);
}

public class FileViewControlViewModel : FileViewControlViewModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly UiStatus _uiStatus;
    private readonly IStorage _storage;
    private readonly IThumbnailsViewer _thumbnailsViewer;
    private readonly IDialogService _dialogService;

    private readonly ObservableCollection<DirectoryTreeViewModel> _rootDirectoryModels = new();
    private readonly ObservableCollection<IThumbnail<object>> _currentItems = new();

    private ActionPipe<DirectoryTreeViewModel> _directoryExpandedActionPipe = new();

    private IFileViewControlCommands? _commands;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CompositeDisposable _disposable = new();

    public FileViewControlViewModel(UiStatus uiStatus, IStorage storage, IThumbnailsViewer thumbnailsViewer, IDialogService dialogService)
    {
        _uiStatus = uiStatus;
        _storage = storage;
        _thumbnailsViewer = thumbnailsViewer;
        _dialogService = dialogService;

        _directoryExpandedActionPipe.Listener.Listen(v => this.OnDirectoryExpanded(v)).AddTo(_disposable);

        this.TreeViewWidth = new ReactivePropertySlim<GridLength>(new GridLength(200));
        this.ThumbnailWidth = new ReactivePropertySlim<int>(256);
        this.ThumbnailHeight = new ReactivePropertySlim<int>(256);
        this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
        this.SelectedDirectory = new ReactivePropertySlim<DirectoryTreeViewModel>().AddTo(_disposable);
        this.SelectedDirectory.Where(n => n is not null).Subscribe(n => this.OnCurrentDirectoryChanged(n)).AddTo(_disposable);
        this.CurrentItems = new ReadOnlyObservableCollection<IThumbnail<object>>(_currentItems);

        this.Init();
    }

    private async void Init()
    {
        await foreach (var directory in _storage.FindDirectoriesAsync())
        {
            _rootDirectoryModels.Add(new DirectoryTreeViewModel(null, directory, _directoryExpandedActionPipe.Caller));
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
        _cancellationTokenSource.Cancel();

        await _thumbnailsViewer.DisposeAsync();

        _cancellationTokenSource.Dispose();
    }

    public override void SetViewCommands(IFileViewControlCommands commands)
    {
        _commands = commands;
    }

    public override void NotifyThumbnailDoubleTapped(object item)
    {
    }

    public override void NotifyThumbnailPrepared(object item)
    {
        if (item is IThumbnail<object> model)
        {
            _thumbnailsViewer.ItemPrepared(model);
        }
    }

    public override void NotifyThumbnailClearing(object item)
    {
        if (item is IThumbnail<object> model)
        {
            _thumbnailsViewer.ItemClearing(model);
        }
    }

    private async void OnDirectoryExpanded(DirectoryTreeViewModel directoryViewModel)
    {
        var children = new List<IDirectory>();

        await foreach (var dir in directoryViewModel.Directory.FindDirectoriesAsync())
        {
            children.Add(dir);
        }

        await foreach (var file in directoryViewModel.Directory.FindFilesAsync())
        {
            if (!file.Attributes.HasFlag(Components.Storages.FileAttributes.Archive)) continue;

            var dir = await file.TryConvertToDirectoryAsync();
            if (dir is null) continue;

            children.Add(dir);
        }

        children.Sort((x, y) => x.Name.CompareTo(y.Name));

        directoryViewModel.SetChildren(children);
    }

    private async void OnCurrentDirectoryChanged(DirectoryTreeViewModel directoryViewModel)
    {
        var oldModels = this.CurrentItems!.ToArray();
        var result = await _thumbnailsViewer.StartAsync(directoryViewModel.Directory, 256, 256, TimeSpan.FromSeconds(1), _cancellationTokenSource.Token);

        _commands!.ScrollToTop();

        _currentItems.Clear();
        _currentItems.AddRange(CollectionHelper.Unite<IThumbnail<object>>(result.FileThumbnails, result.DirectoryThumbnails));

        foreach (var model in oldModels)
        {
            model.Dispose();
        }
    }
}
