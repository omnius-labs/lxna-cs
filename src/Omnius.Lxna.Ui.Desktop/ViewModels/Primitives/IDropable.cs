namespace Omnius.Lxna.Ui.Desktop.Models.Primitives
{
    public interface IDropable
    {
        bool TryAdd(object value);
        bool TryRemove(object value);
    }
}
