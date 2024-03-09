using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Core.Text;
using Omnius.Lxna.Components.Storage;
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
    public ReadOnlyObservableCollection<Thumbnail<object>>? Thumbnails { get; protected set; }
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

    private readonly LxnaEnvironment _lxnaEnvironment;
    private readonly IStorage _storage;
    private readonly ThumbnailsViewer _thumbnailsViewer;
    private readonly IApplicationDispatcher _applicationDispatcher;
    private readonly IDialogService _dialogService;

    private IExplorerViewCommands? _commands;

    private readonly ObservableCollection<Thumbnail<object>> _thumbnails = new();

    private ActionPipe<TreeNodeModel> _isExpandedChangedActionPipe = new();
    private ActionPipe _cancelWaitActionPipe = new();

    private readonly ReactivePropertySlim<bool> _isBusy;

    private readonly AsyncLock _asyncLock = new();

    private readonly CompositeDisposable _disposable = new();

    public ExplorerViewModel(LxnaEnvironment lxnaEnvironment, UiStatus uiStatus, IStorage storage, ThumbnailsViewer ThumbnailsViewer, IApplicationDispatcher applicationDispatcher, IDialogService dialogService)
    {
        _lxnaEnvironment = lxnaEnvironment;
        _storage = storage;
        _thumbnailsViewer = ThumbnailsViewer;
        _applicationDispatcher = applicationDispatcher;
        _dialogService = dialogService;

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
        this.CancelWaitCommand.Subscribe(() => this.OnCancelWait()).AddTo(_disposable);
        this.RootTreeNode = new RootTreeNodeModel(_isExpandedChangedActionPipe.Caller) { Name = "/" };
        this.SelectedTreeNode = new ReactivePropertySlim<TreeNodeModel>().AddTo(_disposable);
        this.SelectedTreeNode.Where(n => n is not null).Subscribe(n => this.OnSelectedTreeNodeModelChanged(n)).AddTo(_disposable);
        this.TreeViewWidth = this.Status.ToReactivePropertySlimAsSynchronized(n => n.TreeViewWidth, convert: ConvertHelper.DoubleToGridLength, convertBack: ConvertHelper.GridLengthToDouble).AddTo(_disposable);
        this.Thumbnails = new ReadOnlyObservableCollection<Thumbnail<object>>(_thumbnails);
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
        if (item is Thumbnail<object> thumbnail && thumbnail.Item is IFile file)
        {
            await _dialogService.ShowPicturePreviewWindowAsync(file);
        }
    }

    public override void NotifyThumbnailPrepared(object item)
    {
        if (item is Thumbnail<object> thumbnail)
        {
            _thumbnailsViewer.ThumbnailPrepared(thumbnail);
        }
    }

    public override void NotifyThumbnailClearing(object item)
    {
        if (item is Thumbnail<object> thumbnail)
        {
            _thumbnailsViewer.ThumbnailClearing(thumbnail);
        }
    }

    private void OnCancelWait()
    {
        _cancelWaitActionPipe.Caller.Call();
    }

    private async void OnIsExpandedChanged(TreeNodeModel expendedTreeNode)
    {
        if (expendedTreeNode.Tag is not IDirectory) return;
        var expendedDirectory = (IDirectory)expendedTreeNode.Tag;

        await Task.Delay(1).ConfigureAwait(false);

        using (await _asyncLock.LockAsync())
        {
            if (expendedTreeNode.Children.Count > 0) return;

            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = true;
            });

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource();
                using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

                var directories = await this.FindDirectories(expendedDirectory, cancellationTokenSource.Token);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    var children = directories.Select(dir =>
                    {
                        return new TreeNodeModel(_isExpandedChangedActionPipe.Caller)
                        {
                            Name = dir.Name,
                            Tag = dir
                        };
                    }).ToArray();

                    expendedTreeNode.AddChildren(children);

                    _isBusy!.Value = false;
                }, DispatcherPriority.Background, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    expendedTreeNode.IsExpanded = false;
                    expendedTreeNode.ClearChildren();
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
            if (!file.Attributes.HasFlag(Components.Storage.FileAttributes.Archive)) continue;

            var archive = await file.TryConvertToDirectoryAsync(cancellationToken);
            if (archive is null) continue;

            archives.Add(archive);
        }

        dirs.Sort((x, y) => LogicalStringComparer.Instance.Compare(x.Name, y.Name));
        archives.Sort((x, y) => LogicalStringComparer.Instance.Compare(x.Name, y.Name));

        return CollectionHelper.Unite(dirs, archives).ToArray();
    }

    private async void OnSelectedTreeNodeModelChanged(TreeNodeModel selectedTreeNode)
    {
        if (selectedTreeNode.Tag is not IDirectory) return;
        var selectedDirectory = (IDirectory)selectedTreeNode.Tag;

        await Task.Delay(1).ConfigureAwait(false);

        using (await _asyncLock.LockAsync())
        {
            await _applicationDispatcher.InvokeAsync(() =>
            {
                selectedTreeNode.IsSelected = true;

                foreach (var treeNode in this.RootTreeNode!.VisibleChildren)
                {
                    if (treeNode == selectedTreeNode) continue;
                    treeNode.IsSelected = false;
                }
            });

            await _applicationDispatcher.InvokeAsync(() =>
            {
                _isBusy!.Value = true;
            });

            using var cancellationTokenSource = new CancellationTokenSource();
            using var unregister = _cancelWaitActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => cancellationTokenSource.Cancel()));

            try
            {
                var comparison = this.GenComparison();

                var result = await _thumbnailsViewer.LoadAsync(selectedDirectory, 256, 256, TimeSpan.FromSeconds(1), comparison, cancellationTokenSource.Token);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _commands!.ThumbnailsScrollToTop();

                    var oldThumbnails = this.Thumbnails!.ToArray();

                    _thumbnails.Clear();
                    _thumbnails.AddRange(result);

                    foreach (var model in oldThumbnails)
                    {
                        model.Dispose();
                    }

                    _isBusy!.Value = false;
                }, DispatcherPriority.Background, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    _commands!.ThumbnailsScrollToTop();

                    var oldThumbnails = this.Thumbnails!.ToArray();

                    _thumbnails.Clear();

                    foreach (var model in oldThumbnails)
                    {
                        model.Dispose();
                    }

                    _isBusy!.Value = false;
                });
            }
        }
    }

    private Comparison<object> GenComparison() => new Comparison<object>((x, y) =>
    {
        if (x is IFile fx)
        {
            if (y is IFile fy) return string.Compare(fx.Name, fy.Name, StringComparison.InvariantCulture);
            else return 1;
        }
        else if (x is IDirectory dx)
        {
            if (y is IDirectory dy) return string.Compare(dx.Name, dy.Name, StringComparison.InvariantCulture);
            else return -1;
        }

        return 0;
    });
}
