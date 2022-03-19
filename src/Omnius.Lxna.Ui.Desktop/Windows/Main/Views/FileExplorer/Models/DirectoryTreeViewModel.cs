using System.Collections.Immutable;
using Omnius.Core.Avalonia;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Storages;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public sealed class DirectoryTreeViewModel : TreeViewModelBase, IDisposable
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IDirectory _model;
    private readonly IActionCaller<TreeViewModelBase> _isExpandedChangedAction;

    private bool _isLoaded;

    public DirectoryTreeViewModel(TreeViewModelBase? parent, IDirectory model, IActionCaller<TreeViewModelBase> isExpandedChangedAction) : base(parent)
    {
        _model = model;
        _isExpandedChangedAction = isExpandedChangedAction;

        this.Children = new[] { EmptyTreeViewModel.Default }.Cast<TreeViewModelBase>().ToImmutableArray();
    }

    public void Dispose()
    {
        foreach (var child in this.Children)
        {
            if (child is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        this.Children = this.Children.Clear();

        _model.Dispose();
    }

    public IDirectory Model => _model;

    public string Name => _model.Name;

    public bool IsLoaded => _isLoaded;

    public override bool TryAdd(object value)
    {
        throw new NotImplementedException();
    }

    public override bool TryRemove(object value)
    {
        throw new NotImplementedException();
    }

    private bool _isExpanded = false;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            this.SetProperty(ref _isExpanded, value);
            _isExpandedChangedAction.Call(this);
        }
    }

    public ImmutableArray<TreeViewModelBase> Children { get; private set; }

    public void SetChildren(IEnumerable<IDirectory> children)
    {
        var builder = ImmutableArray.CreateBuilder<TreeViewModelBase>();

        foreach (var child in children)
        {
            builder.Add(new DirectoryTreeViewModel(this, child, _isExpandedChangedAction));
        }

        var oldChildren = this.Children;
        this.Children = builder.ToImmutable();
        this.RaisePropertyChanged(nameof(this.Children));

        foreach (var child in oldChildren)
        {
            if (child is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _isLoaded = true;
    }

    public void ResetChildren()
    {
        var oldChildren = this.Children;
        this.Children = new[] { EmptyTreeViewModel.Default }.Cast<TreeViewModelBase>().ToImmutableArray();
        this.RaisePropertyChanged(nameof(this.Children));

        foreach (var child in oldChildren)
        {
            if (child is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _isLoaded = false;
    }
}
