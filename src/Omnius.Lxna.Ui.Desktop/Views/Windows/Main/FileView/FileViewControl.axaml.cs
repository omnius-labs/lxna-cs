using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Omnius.Lxna.Ui.Desktop.Views.Windows.Main.FileView
{
    public class FileViewControl : UserControl
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

        public FileViewControlModel? Model
        {
            get => this.DataContext as FileViewControlModel;
            set => this.DataContext = value;
        }

        private void ItemsRepeater_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (this.Model is null) throw new NullReferenceException(nameof(this.Model));

            if (e.Source is IDataContextProvider control)
            {
                this.Model.NotifyDoubleTapped(control.DataContext);
            }
        }

        private void ItemsRepeater_ElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
        {
            if (this.Model is null) throw new NullReferenceException(nameof(this.Model));

            this.Model.NotifyItemPrepared(e.Element.DataContext);
        }

        private void ItemsRepeater_ElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
        {
            if (this.Model is null) throw new NullReferenceException(nameof(this.Model));

            this.Model.NotifyItemClearing(e.Element.DataContext);
        }
    }
}
