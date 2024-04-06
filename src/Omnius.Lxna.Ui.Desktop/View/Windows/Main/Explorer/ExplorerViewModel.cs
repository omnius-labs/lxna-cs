using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Core.Text;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Ui.Desktop.Service.Internal;
using Omnius.Lxna.Ui.Desktop.Service.Thumbnail;
using Omnius.Lxna.Ui.Desktop.Shared;
using Omnius.Lxna.Ui.Desktop.View.Helpers;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public abstract class ExplorerViewModelBase : AsyncDisposableBase
{
    public ExplorerViewStatus? Status { get; protected set; }
    public ReadOnlyReactivePropertySlim<bool>? IsWaiting { get; protected set; }
    public ReactiveCommand? CancelWaitCommand { get; protected set; }
    public RootTreeNodeModel? RootTreeNode { get; protected set; }
    public ReactivePropertySlim<TreeNodeModel>? SelectedTreeNode { get; protected set; }
    public ReactivePropertySlim<GridLength>? TreeViewWidth { get; protected set; }
    public ReactivePropertySlim<int>? ThumbnailWidth { get; protected set; }
    public ReactivePropertySlim<int>? ThumbnailHeight { get; protected set; }

    public AvaloniaList<Thumbnail> Thumbnails { get; } = new();

    public abstract void SetViewCommands(IExplorerViewCommands commands);
    public abstract void NotifyTreeNodeTapped(object item);
    public abstract void NotifyThumbnailDoubleTapped(object item);
    public abstract void NotifyThumbnailsChanged(IEnumerable<object> items);
}

public class ExplorerViewModel : ExplorerViewModelBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly LxnaEnvironment _lxnaEnvironment;
    private readonly IStorage _storage;
    private readonly ThumbnailsViewer _thumbnailsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IDialogService _dialogService;

    private IExplorerViewCommands? _commands;

    private ActionPipe _cancelWaitActionPipe = new();

    private FuncDebouncer<TreeNodeModel> _refreshTreeNodeChildrenDebouncer;
    private ActionDebouncer _refreshThumbnailsDebouncer;

    private readonly ReactivePropertySlim<bool> _isBusy;

    private NestedPath _selectedPath = NestedPath.Empty;

    private readonly AsyncLock _asyncLock = new();

    private readonly CompositeDisposable _disposable = new();

    public ExplorerViewModel(LxnaEnvironment lxnaEnvironment, UiStatus uiStatus, IStorage storage, ThumbnailsViewer ThumbnailsViewer, IApplicationDispatcher applicationDispatcher, IDialogService dialogService)
    {
        _lxnaEnvironment = lxnaEnvironment;
        _storage = storage;
        _thumbnailsViewer = ThumbnailsViewer;
        _applicationDispatcher = applicationDispatcher;
        _dialogService = dialogService;

        _refreshTreeNodeChildrenDebouncer = new FuncDebouncer<TreeNodeModel>(this.RefreshTreeNodeChildren);
        _refreshThumbnailsDebouncer = new ActionDebouncer(this.RefreshThumbnails);

        this.Status = uiStatus.ExplorerView ??= new ExplorerViewStatus();

        _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);
        this.IsWaiting = _isBusy
            // trueに設定されたときのみにフィルタする
            .Where(x => x == true)
            // 遅延を設定
            .SelectMany(_ => Observable.Timer(TimeSpan.FromSeconds(1))
                .TakeUntil(_isBusy.Where(x => x == false))
                .Select(__ => true))
            // falseに設定された場合、即座に結果をfalseにする
            .Merge(_isBusy.Where(x => x == false).Select(x => false))
            // 結果を反映
            .ToReadOnlyReactivePropertySlim();
        this.CancelWaitCommand = new ReactiveCommand().AddTo(_disposable);
        this.CancelWaitCommand.Subscribe(_cancelWaitActionPipe.Caller.Call).AddTo(_disposable);
        this.RootTreeNode = new RootTreeNodeModel(this.OnTreeNodeIsExpandedChanged) { Name = "/" };
        this.SelectedTreeNode = new ReactivePropertySlim<TreeNodeModel>().AddTo(_disposable);
        this.TreeViewWidth = this.Status.ToReactivePropertySlimAsSynchronized(n => n.TreeViewWidth, convert: ConvertHelper.DoubleToGridLength, convertBack: ConvertHelper.GridLengthToDouble).AddTo(_disposable);
        this.ThumbnailWidth = new ReactivePropertySlim<int>(256).AddTo(_disposable);
        this.ThumbnailHeight = new ReactivePropertySlim<int>(256).AddTo(_disposable);

        this.Init();
    }

    private async void Init()
    {
        foreach (var directory in await _storage.FindDirectoriesAsync())
        {
            var child = new TreeNodeModel(this.OnTreeNodeIsExpandedChanged)
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
        if (item is TreeNodeModel node && node.Tag is IDirectory dir)
        {
            _selectedPath = dir.LogicalPath;
            _refreshThumbnailsDebouncer.Signal();
        }
    }

    public override async void NotifyThumbnailDoubleTapped(object item)
    {
        if (item is Thumbnail thumbnail)
        {
            if (thumbnail.Item is IDirectory dir)
            {
                _selectedPath = dir.LogicalPath;
                this.SelectedTreeNode!.Value.IsExpanded = true;
            }
            else if (thumbnail.Item is IFile archiveFile && archiveFile.IsArchive)
            {
                using var archive = await archiveFile.TryConvertToDirectoryAsync().ConfigureAwait(false);
                if (archive is null) return;

                _selectedPath = archive.LogicalPath;
                this.SelectedTreeNode!.Value.IsExpanded = true;
            }
            else if (thumbnail.Item is IFile file)
            {
                var files = this.Thumbnails.Select(x => x.Item).OfType<IFile>().ToArray();
                var position = Array.IndexOf(files, file);
                await _dialogService.ShowPreviewWindowAsync(files, position);
            }
        }
    }

    public override void NotifyThumbnailsChanged(IEnumerable<object> items)
    {
        var thumbnails = items.OfType<Thumbnail>().ToArray();
        _thumbnailsViewer.SetPreparedThumbnails(thumbnails);
    }

    private void OnTreeNodeIsExpandedChanged(TreeNodeModel expendedTreeNode)
    {
        if (expendedTreeNode.Tag is not IDirectory) return;
        if (expendedTreeNode.Children.Count > 0) return;

        _refreshTreeNodeChildrenDebouncer.Signal(expendedTreeNode);
    }

    private async Task RefreshTreeNodeChildren(TreeNodeModel treeNode)
    {
        await Task.Delay(1).ConfigureAwait(false);

        using (await _asyncLock.LockAsync())
        {
            if (treeNode.Tag is not IDirectory) return;
            var dir = (IDirectory)treeNode.Tag;

            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = true;
            }, DispatcherPriority.Background).ConfigureAwait(false);

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

                var directories = await this.FindDirectories(dir, cancellationTokenSource.Token).ConfigureAwait(false);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    var children = directories.Select(dir =>
                    {
                        return new TreeNodeModel(_refreshTreeNodeChildrenDebouncer.Signal)
                        {
                            Name = dir.Name,
                            Tag = dir
                        };
                    }).ToArray();

                    treeNode.ClearChildren();
                    treeNode.AddChildren(children);

                    _isBusy!.Value = false;
                }, DispatcherPriority.Background, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    treeNode.IsExpanded = false;
                    treeNode.ClearChildren();
                    _isBusy!.Value = false;
                }, DispatcherPriority.Background).ConfigureAwait(false);
            }

            if (dir.LogicalPath != _selectedPath)
            {
                _refreshThumbnailsDebouncer.Signal();
            }
        }
    }

    private async Task<IDirectory[]> FindDirectories(IDirectory dir, CancellationToken cancellationToken = default)
    {
        var dirs = new List<IDirectory>();
        dirs.AddRange(await dir.FindDirectoriesAsync(cancellationToken).ConfigureAwait(false));
        dirs.Sort((x, y) => LogicalStringComparer.Instance.Compare(x.Name, y.Name));

        var archives = new List<IDirectory>();

        foreach (var file in await dir.FindFilesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!file.IsArchive) continue;

            var archive = await file.TryConvertToDirectoryAsync(cancellationToken).ConfigureAwait(false);
            if (archive is null) continue;

            archives.Add(archive);
        }

        archives.Sort((x, y) => LogicalStringComparer.Instance.Compare(x.Name, y.Name));

        return CollectionHelper.Unite(dirs, archives).ToArray();
    }

    private async Task RefreshThumbnails()
    {
        await Task.Delay(1).ConfigureAwait(false);

        using (await _asyncLock.LockAsync())
        {
            TreeNodeModel? treeNode = null;
            IDirectory? dir = null;

            await _applicationDispatcher.InvokeAsync(() =>
            {
                foreach (var n in this.RootTreeNode!.VisibleChildren)
                {
                    if (n.Tag is not IDirectory d || d.LogicalPath != _selectedPath) continue;
                    treeNode = n;
                    dir = d;
                }
            }, DispatcherPriority.Background).ConfigureAwait(false);

            if (treeNode is null || dir is null) return;

            await _applicationDispatcher.InvokeAsync(() =>
            {
                treeNode.IsSelected = true;
                _isBusy!.Value = true;
            }, DispatcherPriority.Background).ConfigureAwait(false);

            using var cancellationTokenSource = new CancellationTokenSource();
            using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

            try
            {
                var comparison = this.GenComparison();
                await _thumbnailsViewer.LoadAsync(dir, 256, 256, TimeSpan.FromSeconds(1), comparison, cancellationTokenSource.Token).ConfigureAwait(false);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _commands!.ThumbnailsViewerScrollToTop();

                    var oldThumbnails = this.Thumbnails.ToArray();
                    this.Thumbnails.Clear();
                    oldThumbnails.Dispose();
                }, DispatcherPriority.Background, cancellationTokenSource.Token).ConfigureAwait(false);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    this.Thumbnails.AddRange(_thumbnailsViewer.Thumbnails);

                    this.SelectedTreeNode!.Value = treeNode;

                    _isBusy!.Value = false;
                }, DispatcherPriority.Background, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _commands!.ThumbnailsViewerScrollToTop();

                    var oldThumbnails = this.Thumbnails.ToArray();
                    this.Thumbnails.Clear();
                    oldThumbnails.Dispose();

                    this.SelectedTreeNode!.Value = treeNode;

                    _isBusy!.Value = false;
                }, DispatcherPriority.Background).ConfigureAwait(false);
            }
        }
    }

    private Comparison<object> GenComparison() => new Comparison<object>((x, y) =>
    {
        return (x, y) switch
        {
            (IFile fx, IFile fy) => LogicalStringComparer.Instance.Compare(fx.Name, fy.Name),
            (IDirectory dx, IDirectory dy) when (dx.Attributes & dy.Attributes).HasFlag(DirectoryAttributes.Unknown) => dx.Attributes.CompareTo(dy.Attributes),
            (IDirectory dx, IDirectory dy) => LogicalStringComparer.Instance.Compare(dx.Name, dy.Name),
            _ => 0
        };
    });
}
