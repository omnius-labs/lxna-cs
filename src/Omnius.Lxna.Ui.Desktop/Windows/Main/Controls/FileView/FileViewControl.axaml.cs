using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Core.Avalonia;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main;

public class FileViewControl : StatefulUserControl<FileViewControlViewModelBase>
{
    public FileViewControl()
    {
        this.InitializeComponent();

        var itemsRepeater = this.FindControl<ItemsRepeater>("ItemsRepeater");
        itemsRepeater.DoubleTapped += this.ItemsRepeater_DoubleTapped;
        itemsRepeater.ElementPrepared += this.ItemsRepeater_ElementPrepared;
        itemsRepeater.ElementClearing += this.ItemsRepeater_ElementClearing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ItemsRepeater_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (e.Source is IDataContextProvider control)
        {
            if (control.DataContext is null) return;
            this.ViewModel?.NotifyItemDoubleTapped(control.DataContext);
        }
    }

    private void ItemsRepeater_ElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        this.ViewModel?.NotifyItemPrepared(e.Element.DataContext);
    }

    private void ItemsRepeater_ElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element.DataContext is null) return;
        this.ViewModel?.NotifyItemClearing(e.Element.DataContext);
    }
}
