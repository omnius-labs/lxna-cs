using System.Reactive.Disposables;
using Avalonia.Controls;
using Omnius.Core.Avalonia;
using Omnius.Core.Pipelines;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.View.Windows;

public class ExplorerViewDesignModel : ExplorerViewModelBase
{
    private readonly CompositeDisposable _disposable = new();

    public ExplorerViewDesignModel()
    {
        var _isBusy = new ReactivePropertySlim<bool>(false).AddTo(_disposable);
        this.IsWaiting = _isBusy.ToReadOnlyReactivePropertySlim().AddTo(_disposable);
        this.TreeViewWidth = new ReactivePropertySlim<GridLength>(new GridLength(240)).AddTo(_disposable);

        var rootTreeNode = new RootTreeNodeModel((_) => { })
        {
            Name = "/",
        };
        rootTreeNode.AddChild(new RootTreeNodeModel((_) => { })
        {
            Name = @"C:\",
        });
        rootTreeNode.AddChild(new RootTreeNodeModel((_) => { })
        {
            Name = @"D:\",
        });
        this.RootTreeNode = rootTreeNode;
        this.RootTreeNode.Update();
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _disposable.Dispose();
    }
}
