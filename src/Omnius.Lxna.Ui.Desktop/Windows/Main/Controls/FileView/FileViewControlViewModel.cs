using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls;
using Omnius.Core;
using Reactive.Bindings;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Ui.Desktop.Interactors.Internal;
using Omnius.Lxna.Ui.Desktop.Internal;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Ui.Desktop.Internal.Models;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class FileViewControlViewModelBase : AsyncDisposableBase
{

    public ReactivePropertySlim<GridLength>? TreeViewWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailHeight { get; protected set; }

    public ReadOnlyReactiveCollection<DirectoryViewModel>? RootDirectories { get; protected set; }

    public ReactiveProperty<DirectoryViewModel>? SelectedDirectory { get; protected set; }

    public ReadOnlyReactiveCollection<IThumbnail>? CurrentItems { get; protected set; }

    public abstract void NotifyItemDoubleTapped(object item);

    public abstract void NotifyItemPrepared(object item);

    public abstract void NotifyItemClearing(object item);
}

public class FileViewControlViewModel : FileViewControlViewModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly UiStatus _uiStatus;
    private readonly IStorage _storage;
    private readonly IThumbnailsViewer _thumbnailsViewer;
    private readonly IDialogService _dialogService;

    private readonly ObservableCollection<DirectoryViewModel> _rootDirectoryModels = new();
    private readonly ObservableCollection<IThumbnail> _currentItems = new();

    private ActionPipe<DirectoryViewModel> _directoryExpandedActionPipe = new();

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
        this.SelectedDirectory = new ReactiveProperty<DirectoryViewModel>().AddTo(_disposable);
        this.SelectedDirectory.Where(n => n is not null).Subscribe(n => this.OnCurrentDirectoryChanged(n)).AddTo(_disposable);
        this.CurrentItems = _currentItems.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);

        this.Init();
    }

    private async void Init()
    {
        await foreach (var directory in _storage.FindDirectoriesAsync("temp", BytesPool.Shared))
        {
            _rootDirectoryModels.Add(new DirectoryViewModel(null, directory, _directoryExpandedActionPipe.Caller));
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
        _cancellationTokenSource.Cancel();

        await _thumbnailsViewer.DisposeAsync();

        _cancellationTokenSource.Dispose();
    }

    public override void NotifyItemDoubleTapped(object item)
    {
        if (item is not IThumbnail model) return;
    }

    public override void NotifyItemPrepared(object item)
    {
        if (item is IThumbnail model)
        {
            _thumbnailsViewer.ItemPrepared(model);
        }
    }

    public override void NotifyItemClearing(object item)
    {
        if (item is IThumbnail model)
        {
            _thumbnailsViewer.ItemClearing(model);
        }
    }

    private async void OnDirectoryExpanded(DirectoryViewModel directoryViewModel)
    {
        var children = new List<IDirectory>();

        await foreach (var child in directoryViewModel.Directory.FindDirectoriesAsync())
        {
            children.Add(child);
        }

        directoryViewModel.SetChildren(children);
    }

    private async void OnCurrentDirectoryChanged(DirectoryViewModel directoryViewModel)
    {
        var oldModels = _currentItems;
        var newModels = await _thumbnailsViewer.StartAsync(directoryViewModel.Directory, 256, 256, _cancellationTokenSource.Token);

        _currentItems.Clear();
        _currentItems.AddRange(newModels);

        foreach (var model in oldModels)
        {
            model.Dispose();
        }
    }
}
