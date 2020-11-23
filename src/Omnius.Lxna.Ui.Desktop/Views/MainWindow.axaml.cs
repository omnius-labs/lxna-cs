using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Omnius.Lxna.Components;
using Omnius.Lxna.Ui.Desktop.ViewModels;

namespace Omnius.Lxna.Ui.Desktop.Views
{
    public class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
            : this()
        {
            _viewModel = viewModel;
            this.DataContext = _viewModel;

            var searchControl = this.FindControl<SearchControl>("SearchControl");
            searchControl.ViewModel = _viewModel.SearchControlViewModel;
        }

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
