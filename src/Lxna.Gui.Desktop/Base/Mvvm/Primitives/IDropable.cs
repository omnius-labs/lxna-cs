namespace Lxna.Gui.Desktop.Base.Mvvm.Primitives
{
    public interface IDropable
    {
        bool TryAdd(object value);
        bool TryRemove(object value);
    }
}
