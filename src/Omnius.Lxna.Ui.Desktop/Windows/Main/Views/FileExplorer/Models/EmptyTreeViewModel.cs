using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public sealed class EmptyTreeViewModel : TreeViewModelBase
{
    public static EmptyTreeViewModel Default { get; } = new EmptyTreeViewModel();

    public EmptyTreeViewModel() : base(null)
    {
    }

    public override bool TryAdd(object value)
    {
        throw new NotImplementedException();
    }

    public override bool TryRemove(object value)
    {
        throw new NotImplementedException();
    }
}
