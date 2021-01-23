using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Lxna.Ui.Desktop.ViewModels;

namespace Omnius.Lxna.Ui.Desktop.Views
{
    public class SearchControl : UserControl
    {
        public SearchControl()
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

        public SearchControlViewModel? ViewModel
        {
            get => this.DataContext as SearchControlViewModel;
            set => this.DataContext = value;
        }

        private void ItemsRepeater_DoubleTapped(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (e.Source is IDataContextProvider control)
            {
                this.ViewModel.NotifyDoubleTapped(control.DataContext);
            }
        }

        private void ItemsRepeater_ElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
        {
            if (this.ViewModel.CurrentItems.Count >= 2)
            {
                if (e.Element.DataContext == this.ViewModel.CurrentItems[0])
                {
                    return;
                }
                else if (e.Element.DataContext == this.ViewModel.CurrentItems[1])
                {
                    this.ViewModel.NotifyItemPrepared(this.ViewModel.CurrentItems[0]);
                }
            }

            this.ViewModel.NotifyItemPrepared(e.Element.DataContext);
        }

        private void ItemsRepeater_ElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
        {
            if (this.ViewModel.CurrentItems.Count >= 2)
            {
                if (e.Element.DataContext == this.ViewModel.CurrentItems[0])
                {
                    return;
                }
                else if (e.Element.DataContext == this.ViewModel.CurrentItems[1])
                {
                    this.ViewModel.NotifyItemClearing(this.ViewModel.CurrentItems[0]);
                }
            }

            this.ViewModel.NotifyItemClearing(e.Element.DataContext);
        }
    }
}
