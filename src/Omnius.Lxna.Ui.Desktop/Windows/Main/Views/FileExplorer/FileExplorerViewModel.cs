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
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class FileExplorerViewModelBase : AsyncDisposableBase
{
    public ReadOnlyReactivePropertySlim<bool>? IsWaiting { get; protected set; }

    public ReactiveCommand? CancelWait { get; protected set; }

    public ReactivePropertySlim<GridLength>? TreeViewWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailHeight { get; protected set; }

    public ReadOnlyReactiveCollection<DirectoryTreeViewModel>? RootDirectories { get; protected set; }

    public ReactivePropertySlim<DirectoryTreeViewModel>? SelectedDirectory { get; protected set; }

    public ReadOnlyObservableCollection<IThumbnail<object>>? CurrentItems { get; protected set; }

    public abstract void SetViewCommands(IFileExplorerViewCommands commands);

    public abstract void NotifyThumbnailDoubleTapped(object item);

    public abstract void NotifyThumbnailPrepared(object item);

    public abstract void NotifyThumbnailClearing(object item);
}

public class FileExplorerViewModel : FileExplorerViewModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly UiStatus _uiStatus;
    private readonly IStorage _storage;
    private readonly IThumbnailsViewer _thumbnailsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IDialogService _dialogService;

    private IFileExplorerViewCommands? _commands;

    private readonly ObservableCollection<DirectoryTreeViewModel> _rootDirectoryModels = new();
    private readonly ObservableCollection<IThumbnail<object>> _currentItems = new();

    private ActionPipe<DirectoryTreeViewModel> _loadDirectoryActionPipe = new();
    private ActionPipe _cancelWaitActionPipe = new();

    private readonly ReactivePropertySlim<bool> _isBusy;

    private readonly AsyncLock _asyncLock = new();

    private readonly CompositeDisposable _disposable = new();

    public FileExplorerViewModel(UiStatus uiStatus, IStorage storage, IThumbnailsViewer thumbnailsViewer, IApplicationDispatcher applicationDispatcher, IDialogService dialogService)
    {
        _uiStatus = uiStatus;
        _storage = storage;
        _thumbnailsViewer = thumbnailsViewer;
        _applicationDispatcher = applicationDispatcher;
        _dialogService = dialogService;

        _loadDirectoryActionPipe.Listener.Listen(v => this.OnLoadDirectory(v)).AddTo(_disposable);

        _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);

        this.IsWaiting = _isBusy.DelayWhen(TimeSpan.FromMilliseconds(500), x => x).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        this.CancelWait = new ReactiveCommand().AddTo(_disposable);
        this.CancelWait.Subscribe(() => this.OnCancelWait()).AddTo(_disposable);
        this.TreeViewWidth = new ReactivePropertySlim<GridLength>(new GridLength(200)).AddTo(_disposable);
        this.ThumbnailWidth = new ReactivePropertySlim<int>(256).AddTo(_disposable);
        this.ThumbnailHeight = new ReactivePropertySlim<int>(256).AddTo(_disposable);
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
            _rootDirectoryModels.Add(new DirectoryTreeViewModel(null, directory, _loadDirectoryActionPipe.Caller));
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();

        await _thumbnailsViewer.DisposeAsync();
    }

    public override void SetViewCommands(IFileExplorerViewCommands commands)
    {
        _commands = commands;
    }

    public override async void NotifyThumbnailDoubleTapped(object item)
    {
        if (item is IThumbnail<IFile> model)
        {
            await _dialogService.ShowPicturePreviewWindowAsync(model.Target);
        }
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

    private void OnCancelWait()
    {
        _cancelWaitActionPipe.Caller.Call();
    }

    private async void OnLoadDirectory(DirectoryTreeViewModel directoryViewModel)
    {
        await Task.Delay(1).ConfigureAwait(false);

        using (await _asyncLock.LockAsync())
        {
            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = true;
            });

            IDirectory[]? children = null;

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                using var unregister = _cancelWaitActionPipe.Listener.Listen(() => cancellationTokenSource.Cancel());

                children = await this.FindDirectories(directoryViewModel.Directory, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    directoryViewModel.IsExpanded = false;
                    _isBusy!.Value = false;
                });

                return;
            }

            await _applicationDispatcher.InvokeAsync(() =>
            {
                directoryViewModel.SetChildren(children);
                _isBusy!.Value = false;
            });
        }
    }

    private async Task<IDirectory[]> FindDirectories(IDirectory directory, CancellationToken cancellationToken = default)
    {
        var children = new List<IDirectory>();

        await foreach (var dir in directory.FindDirectoriesAsync(cancellationToken))
        {
            children.Add(dir);
        }

        await foreach (var file in directory.FindFilesAsync(cancellationToken))
        {
            if (!file.Attributes.HasFlag(Components.Storages.FileAttributes.Archive)) continue;

            var dir = await file.TryConvertToDirectoryAsync(cancellationToken);
            if (dir is null) continue;

            children.Add(dir);
        }

        children.Sort((x, y) => x.Name.CompareTo(y.Name));

        return children.ToArray();
    }

    private async void OnCurrentDirectoryChanged(DirectoryTreeViewModel directoryViewModel)
    {
        await Task.Delay(1).ConfigureAwait(false);

        using (await _asyncLock.LockAsync())
        {
            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = true;
            });

            ThumbnailsViewerStartResult result = default;

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                using var unregister = _cancelWaitActionPipe.Listener.Listen(() => cancellationTokenSource.Cancel());

                result = await _thumbnailsViewer.StartAsync(directoryViewModel.Directory, 256, 256, TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _isBusy!.Value = false;
                });

                return;
            }

            await _applicationDispatcher.InvokeAsync(() =>
            {
                _commands!.ScrollToTop();

                var oldModels = this.CurrentItems!.ToArray();

                _currentItems.Clear();
                _currentItems.AddRange(CollectionHelper.Unite<IThumbnail<object>>(result.FileThumbnails, result.DirectoryThumbnails));

                foreach (var model in oldModels)
                {
                    model.Dispose();
                }

                _isBusy!.Value = false;
            });
        }
    }
}
