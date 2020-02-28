using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Omnius.Core;
using Omnius.Core.Data;
using Omnius.Lxna.Service;

namespace Omnius.Lxna.Ui.Desktop.Views.Main
{
    public class MainView : Window
    {
        public MainView()
        {
            var configPath = Path.Combine(Directory.GetCurrentDirectory(), "config");
            Directory.CreateDirectory(configPath);
            var thumbnailGenerator = ThumbnailGenerator.Factory.CreateAsync(configPath, ObjectStore.Factory, BytesPool.Shared).Result;
            this.DataContext = new MainViewModel(thumbnailGenerator);

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
