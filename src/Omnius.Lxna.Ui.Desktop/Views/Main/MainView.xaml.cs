using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Omnius.Lxna.Ui.Desktop.Views.Main
{
    public class MainView : Window
    {
        private MainViewModel _viewModel;

        public MainView(MainViewModel viewModel) : this()
        {
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        public MainView()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

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
                _viewModel.NotifyDoubleTapped(control.DataContext);
            }
        }

        private void ItemsRepeater_ElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
        {
            if (_viewModel.CurrentItems.Count >= 2)
            {
                if (e.Element.DataContext == _viewModel.CurrentItems[0])
                {
                    return;
                }
                else if (e.Element.DataContext == _viewModel.CurrentItems[1])
                {
                    _viewModel.NotifyItemPrepared(_viewModel.CurrentItems[0]);
                }
            }

            _viewModel.NotifyItemPrepared(e.Element.DataContext);
        }

        private void ItemsRepeater_ElementClearing(object? sender, ItemsRepeaterElementClearingEventArgs e)
        {
            if (_viewModel.CurrentItems.Count >= 2)
            {
                if (e.Element.DataContext == _viewModel.CurrentItems[0])
                {
                    return;
                }
                else if (e.Element.DataContext == _viewModel.CurrentItems[1])
                {
                    _viewModel.NotifyItemClearing(_viewModel.CurrentItems[0]);
                }
            }

            _viewModel.NotifyItemClearing(e.Element.DataContext);
        }

        protected override async void OnClosed(EventArgs e)
        {
            if (this.DataContext is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }
        }
    }
}
