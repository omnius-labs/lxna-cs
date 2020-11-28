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

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            var searchControl = this.FindControl<SearchControl>("SearchControl");
            searchControl.ViewModel = this.ViewModel.SearchControlViewModel;
        }

        public MainWindowViewModel? ViewModel
        {
            get => this.DataContext as MainWindowViewModel;
            set => this.DataContext = value;
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
