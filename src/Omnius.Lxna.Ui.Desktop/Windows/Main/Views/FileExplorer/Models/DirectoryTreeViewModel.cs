using System.Collections.Immutable;
using Omnius.Core.Avalonia;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Storages;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public sealed class DirectoryTreeViewModel : TreeViewModelBase, IDisposable
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IDirectory _directory;
    private readonly IActionCaller<DirectoryTreeViewModel> _expanded;

    public DirectoryTreeViewModel(TreeViewModelBase? parent, IDirectory directory, IActionCaller<DirectoryTreeViewModel> expanded) : base(parent)
    {
        _directory = directory;
        _expanded = expanded;

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

        _directory.Dispose();
    }

    public IDirectory Directory => _directory;

    public string Name => _directory.Name;

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
            if (value) _expanded.Call(this);
        }
    }

    public ImmutableArray<TreeViewModelBase> Children { get; private set; }

    public void SetChildren(IEnumerable<IDirectory> children)
    {
        foreach (var child in this.Children)
        {
            if (child is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        var builder = ImmutableArray.CreateBuilder<TreeViewModelBase>();

        foreach (var child in children)
        {
            builder.Add(new DirectoryTreeViewModel(this, child, _expanded));
        }

        this.Children = builder.ToImmutable();

        this.RaisePropertyChanged(nameof(this.Children));
    }
}
