using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Core.Text;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Helpers;
using Omnius.Lxna.Ui.Desktop.Interactors.Internal;
using Omnius.Lxna.Ui.Desktop.Internal;
using Omnius.Lxna.Ui.Desktop.Internal.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public abstract class ExplorerViewModelBase : AsyncDisposableBase
{
    public ExplorerViewStatus? Status { get; protected set; }
    public ReadOnlyReactivePropertySlim<bool>? IsWaiting { get; protected set; }
    public ReactiveCommand? CancelWaitCommand { get; protected set; }
    public RootTreeNodeModel? RootTreeNode { get; protected set; }
    public ReactivePropertySlim<TreeNodeModel>? SelectedTreeNode { get; protected set; }
    public ReactivePropertySlim<GridLength>? TreeViewWidth { get; protected set; }
    public ReadOnlyObservableCollection<Thumbnail>? Thumbnails { get; protected set; }
    public ReactivePropertySlim<int>? ThumbnailWidth { get; protected set; }
    public ReactivePropertySlim<int>? ThumbnailHeight { get; protected set; }

    public abstract void SetViewCommands(IExplorerViewCommands commands);
    public abstract void NotifyTreeNodeTapped(object item);
    public abstract void NotifyThumbnailDoubleTapped(object item);
    public abstract void NotifyThumbnailPrepared(object item);
    public abstract void NotifyThumbnailClearing(object item);
}

public class ExplorerViewModel : ExplorerViewModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IStorage _storage;
    private readonly ThumbnailsViewer _thumbnailsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IDialogService _dialogService;

    private IExplorerViewCommands? _commands;

    private readonly ObservableCollection<Thumbnail> _thumbnails = new();
    private NestedPath? _wantSelectingLogicalPath = null;

    private ActionPipe<TreeNodeModel> _isExpandedChangedActionPipe = new();
    private ActionPipe _cancelWaitActionPipe = new();

    private readonly ReactivePropertySlim<bool> _isBusy;

    private readonly AsyncLock _asyncLock = new();

    private readonly CompositeDisposable _disposable = new();

    public ExplorerViewModel(UiStatus uiStatus, IStorage storage, ThumbnailsViewer ThumbnailsViewer, IApplicationDispatcher applicationDispatcher, IDialogService dialogService)
    {
        _storage = storage;
        _thumbnailsViewer = ThumbnailsViewer;
        _applicationDispatcher = applicationDispatcher;
        _dialogService = dialogService;

        this.Status = uiStatus.ExplorerView ??= new ExplorerViewStatus();

        _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);
        this.IsWaiting = _isBusy.DelayWhen(TimeSpan.FromMilliseconds(500), n => n).ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        this.CancelWaitCommand = new ReactiveCommand().AddTo(_disposable);
        this.CancelWaitCommand.Subscribe(() => this.OnCancelWait()).AddTo(_disposable);
        this.RootTreeNode = new RootTreeNodeModel(_isExpandedChangedActionPipe.Caller);
        this.RootTreeNode.Name = "/";
        this.SelectedTreeNode = new ReactivePropertySlim<TreeNodeModel>().AddTo(_disposable);
        this.SelectedTreeNode.Where(n => n is not null).Subscribe(n => this.OnSelectedTreeViewModelChanged(n)).AddTo(_disposable);
        this.TreeViewWidth = this.Status.ToReactivePropertySlimAsSynchronized(n => n.TreeViewWidth, convert: ConvertHelper.DoubleToGridLength, convertBack: ConvertHelper.GridLengthToDouble).AddTo(_disposable);
        this.Thumbnails = new ReadOnlyObservableCollection<Thumbnail>(_thumbnails);
        this.ThumbnailWidth = new ReactivePropertySlim<int>(256).AddTo(_disposable);
        this.ThumbnailHeight = new ReactivePropertySlim<int>(256).AddTo(_disposable);

        _isExpandedChangedActionPipe.Listener.Listen(v => this.OnIsExpandedChanged(v)).AddTo(_disposable);

        this.Init();
    }

    private async void Init()
    {
        foreach (var directory in await _storage.FindDirectoriesAsync())
        {
            var child = new TreeNodeModel(_isExpandedChangedActionPipe.Caller)
            {
                Name = directory.Name,
                Tag = directory
            };
            this.RootTreeNode!.AddChild(child);
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();

        await _thumbnailsViewer.DisposeAsync();
    }

    public override void SetViewCommands(IExplorerViewCommands commands)
    {
        _commands = commands;
    }

    public override void NotifyTreeNodeTapped(object item)
    {
        if (item is TreeNodeModel node && node.Tag is IDirectory directory)
        {
            this.SelectedTreeNode!.Value = node;
        }
    }

    public override async void NotifyThumbnailDoubleTapped(object item)
    {
        if (item is Thumbnail thumbnail)
        {
            await _dialogService.ShowPicturePreviewWindowAsync(thumbnail.File);
        }
    }

    public override void NotifyThumbnailPrepared(object item)
    {
        if (item is Thumbnail thumbnail)
        {
            _thumbnailsViewer.ThumbnailPrepared(thumbnail);
        }
    }

    public override void NotifyThumbnailClearing(object item)
    {
        if (item is Thumbnail thumbnail)
        {
            _thumbnailsViewer.ThumbnailClearing(thumbnail);
        }
    }

    private void OnCancelWait()
    {
        _cancelWaitActionPipe.Caller.Call();
    }

    private async void OnIsExpandedChanged(TreeNodeModel expendedTree)
    {
        if (expendedTree.Children.Count > 0) return;

        await Task.Delay(1).ConfigureAwait(false);

        if (expendedTree.Tag is IDirectory expendedDirectory)
        {
            using (await _asyncLock.LockAsync())
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _isBusy!.Value = true;
                });

                {
                    IDirectory[]? directories = null;

                    try
                    {
                        using var cancellationTokenSource = new CancellationTokenSource();
                        using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

                        directories = await this.FindDirectories(expendedDirectory, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        await _applicationDispatcher.InvokeAsync(() =>
                        {
                            expendedTree.IsExpanded = false;
                            _isBusy!.Value = false;
                        });

                        return;
                    }

                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        foreach (var dir in directories)
                        {
                            var child = new TreeNodeModel(_isExpandedChangedActionPipe.Caller);
                            child.Name = dir.Name;
                            child.Tag = dir;
                            expendedTree.AddChild(child);
                        }
                    });
                }

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    if (_wantSelectingLogicalPath is not null)
                    {
                        if (_wantSelectingLogicalPath != expendedDirectory.LogicalPath)
                        {
                            foreach (var child in expendedTree.Children)
                            {
                                if (child.Tag is IDirectory selectedDirectory)
                                {
                                    if (selectedDirectory.LogicalPath != _wantSelectingLogicalPath) continue;
                                    this.SelectedTreeNode!.Value = child;
                                    break;
                                }
                            }
                        }

                        _wantSelectingLogicalPath = null;
                    }
                    else
                    {
                        this.SelectedTreeNode!.Value = expendedTree;
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

        dirs.Sort((x, y) => LogicalStringComparer.Instance.Compare(x.Name, y.Name));
        archives.Sort((x, y) => LogicalStringComparer.Instance.Compare(x.Name, y.Name));

        return CollectionHelper.Unite(dirs, archives).ToArray();
    }

    private async void OnSelectedTreeViewModelChanged(TreeNodeModel selectedTreeNode)
    {
        await Task.Delay(1).ConfigureAwait(false);

        if (selectedTreeNode.Tag is IDirectory selectedDirectory)
        {
            using (await _asyncLock.LockAsync())
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _isBusy!.Value = true;

                    selectedTreeNode.IsSelected = true;

                    foreach (var treeNode in this.RootTreeNode!.VisibleChildren)
                    {
                        if (treeNode == selectedTreeNode) continue;
                        treeNode.IsSelected = false;
                    }
                });

                ThumbnailsViewerLoadResult result = default;

                try
                {
                    using var cancellationTokenSource = new CancellationTokenSource();
                    using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

                    Comparison<IFile> comparison;
                    {
                        comparison = new Comparison<IFile>((x, y) => string.Compare(x.Name, y.Name, StringComparison.InvariantCulture));
                    }

                    result = await _thumbnailsViewer.LoadAsync(selectedDirectory, 256, 256, TimeSpan.FromSeconds(1), comparison, cancellationTokenSource.Token);
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
                    _commands!.ThumbnailsScrollToTop();

                    var oldThumbnails = this.Thumbnails!.ToArray();

                    _thumbnails.Clear();
                    _thumbnails.AddRange(result.Thumbnails);

                    foreach (var model in oldThumbnails)
                    {
                        model.Dispose();
                    }

                    _isBusy!.Value = false;
                });
            }
        }
    }
}
