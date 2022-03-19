using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.Storages.Models;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Helpers;
using Omnius.Lxna.Ui.Desktop.Interactors.Internal;
using Omnius.Lxna.Ui.Desktop.Internal;
using Omnius.Lxna.Ui.Desktop.Internal.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class FileExplorerViewModelBase : AsyncDisposableBase
{
    public FileExplorerViewStatus? Status { get; protected set; }

    public ReadOnlyReactivePropertySlim<bool>? IsWaiting { get; protected set; }

    public ReactiveCommand? CancelWaitCommand { get; protected set; }

    public ReactivePropertySlim<GridLength>? TreeViewWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailWidth { get; protected set; }

    public ReactivePropertySlim<int>? ThumbnailHeight { get; protected set; }

    public ReadOnlyReactiveCollection<TreeViewModelBase>? RootTreeViewModels { get; protected set; }

    public ReactivePropertySlim<TreeViewModelBase>? SelectedTreeViewModel { get; protected set; }

    public ReadOnlyObservableCollection<IThumbnail<object>>? Thumbnails { get; protected set; }

    public abstract void SetViewCommands(IFileExplorerViewCommands commands);

    public abstract void NotifyThumbnailDoubleTapped(object item);

    public abstract void NotifyThumbnailPrepared(object item);

    public abstract void NotifyThumbnailClearing(object item);
}

public class FileExplorerViewModel : FileExplorerViewModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IStorage _storage;
    private readonly IThumbnailsViewer _thumbnailsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IDialogService _dialogService;

    private IFileExplorerViewCommands? _commands;

    private readonly ObservableCollection<TreeViewModelBase> _rootTreeViewModels = new();
    private readonly ObservableCollection<IThumbnail<object>> _thumbnails = new();
    private NestedPath? _wantSelectedChildDirectoryLogicalPath = null;

    private ActionPipe<TreeViewModelBase> _isExpandedChangedActionPipe = new();
    private ActionPipe _cancelWaitActionPipe = new();

    private readonly ReactivePropertySlim<bool> _isBusy;

    private readonly AsyncLock _asyncLock = new();

    private readonly CompositeDisposable _disposable = new();

    public FileExplorerViewModel(UiStatus uiStatus, IStorage storage, IThumbnailsViewer thumbnailsViewer, IApplicationDispatcher applicationDispatcher, IDialogService dialogService)
    {
        _storage = storage;
        _thumbnailsViewer = thumbnailsViewer;
        _applicationDispatcher = applicationDispatcher;
        _dialogService = dialogService;

        this.Status = uiStatus.FileExplorerView ??= new FileExplorerViewStatus();

        _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);
        this.IsWaiting = _isBusy.DelayWhen(TimeSpan.FromMilliseconds(500), x => x).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        this.CancelWaitCommand = new ReactiveCommand().AddTo(_disposable);
        this.CancelWaitCommand.Subscribe(() => this.OnCancelWait()).AddTo(_disposable);
        this.TreeViewWidth = this.Status.ToReactivePropertySlimAsSynchronized(n => n.TreeViewWidth, convert: ConvertHelper.DoubleToGridLength, convertBack: ConvertHelper.GridLengthToDouble).AddTo(_disposable);
        this.ThumbnailWidth = new ReactivePropertySlim<int>(256).AddTo(_disposable);
        this.ThumbnailHeight = new ReactivePropertySlim<int>(256).AddTo(_disposable);
        this.RootTreeViewModels = _rootTreeViewModels.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
        this.SelectedTreeViewModel = new ReactivePropertySlim<TreeViewModelBase>().AddTo(_disposable);
        this.SelectedTreeViewModel.Where(n => n is not null).Subscribe(n => this.OnSelectedTreeViewModelChanged(n)).AddTo(_disposable);
        this.Thumbnails = new ReadOnlyObservableCollection<IThumbnail<object>>(_thumbnails);

        _isExpandedChangedActionPipe.Listener.Listen(v => this.OnIsExpandedChanged(v)).AddTo(_disposable);

        this.Init();
    }

    private async void Init()
    {
        foreach (var directory in await _storage.FindDirectoriesAsync())
        {
            _rootTreeViewModels.Add(new DirectoryTreeViewModel(null, directory, _isExpandedChangedActionPipe.Caller));
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
        if (item is IThumbnail<IFile> fileThumbnail)
        {
            await _dialogService.ShowPicturePreviewWindowAsync(fileThumbnail.Target);
        }
        else if (item is IThumbnail<IDirectory> directoryThumbnail)
        {
            if (this.SelectedTreeViewModel!.Value is DirectoryTreeViewModel selectedTreeViewModel)
            {
                _wantSelectedChildDirectoryLogicalPath = directoryThumbnail.Target.LogicalPath;
                selectedTreeViewModel.IsExpanded = true;
            }
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

    private async void OnIsExpandedChanged(TreeViewModelBase treeViewModel)
    {
        await Task.Delay(1).ConfigureAwait(false);

        if (treeViewModel is DirectoryTreeViewModel directoryTreeViewModel)
        {
            using (await _asyncLock.LockAsync())
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _isBusy!.Value = true;
                });

                if (!directoryTreeViewModel.IsLoaded)
                {
                    IDirectory[]? children = null;

                    try
                    {
                        using var cancellationTokenSource = new CancellationTokenSource();
                        using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

                        children = await this.FindDirectories(directoryTreeViewModel.Model, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        await _applicationDispatcher.InvokeAsync(() =>
                        {
                            directoryTreeViewModel.IsExpanded = false;
                            _isBusy!.Value = false;
                        });

                        return;
                    }

                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        directoryTreeViewModel.SetChildren(children);
                    });
                }

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    if (_wantSelectedChildDirectoryLogicalPath is NestedPath childDirectoryLogicalPath)
                    {
                        if (childDirectoryLogicalPath != directoryTreeViewModel.Model.LogicalPath)
                        {
                            foreach (var child in directoryTreeViewModel.Children.OfType<DirectoryTreeViewModel>())
                            {
                                if (child.Model.LogicalPath != _wantSelectedChildDirectoryLogicalPath) continue;
                                this.SelectedTreeViewModel!.Value = child;

                                break;
                            }
                        }

                        _wantSelectedChildDirectoryLogicalPath = null;
                    }
                    else
                    {
                        this.SelectedTreeViewModel!.Value = directoryTreeViewModel;
                    }

                    _isBusy!.Value = false;
                });
            }
        }
    }

    private async Task<IDirectory[]> FindDirectories(IDirectory directory, CancellationToken cancellationToken = default)
    {
        var dirs = new List<IDirectory>();

        foreach (var dir in await directory.FindDirectoriesAsync(cancellationToken))
        {
            dirs.Add(dir);
        }

        var archives = new List<IDirectory>();

        foreach (var file in await directory.FindFilesAsync(cancellationToken))
        {
            if (!file.Attributes.HasFlag(Components.Storages.FileAttributes.Archive)) continue;

            var archive = await file.TryConvertToDirectoryAsync(cancellationToken);
            if (archive is null) continue;

            archives.Add(archive);
        }

        dirs.Sort((x, y) => x.Name.CompareTo(y.Name));
        archives.Sort((x, y) => x.Name.CompareTo(y.Name));

        return CollectionHelper.Unite(dirs, archives).ToArray();
    }

    private async void OnSelectedTreeViewModelChanged(TreeViewModelBase treeViewModel)
    {
        await Task.Delay(1).ConfigureAwait(false);

        if (treeViewModel is DirectoryTreeViewModel directoryTreeViewModel)
        {
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
                    using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

                    result = await _thumbnailsViewer.StartAsync(directoryTreeViewModel.Model, 256, 256, TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
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

                    var oldModels = this.Thumbnails!.ToArray();

                    _thumbnails.Clear();
                    _thumbnails.AddRange(CollectionHelper.Unite<IThumbnail<object>>(result.DirectoryThumbnails, result.FileThumbnails));

                    foreach (var model in oldModels)
                    {
                        model.Dispose();
                    }

                    _isBusy!.Value = false;
                });
            }
        }
    }
}
