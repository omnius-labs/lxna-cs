using System;
using System.IO;
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

            this.Init();
        }

        private async void Init()
        {
            var configPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "../config");
            Directory.CreateDirectory(configPath);

            var archiveFileExtractorOptions = new ArchiveFileExtractorOptions()
            {
                TemporaryDirectoryPath = "./tmp",
            };
            var archiveFileExtractor = await ArchiveFileExtractor.Factory.CreateAsync(archiveFileExtractorOptions);

            var fileSystemOptions = new FileSystemOptions()
            {
                ArchiveFileExtractor = archiveFileExtractor,
            };
            var fileSystem = await FileSystem.Factory.CreateAsync(fileSystemOptions);

            var thumbnailGeneratorOptions = new ThumbnailGeneratorOptions()
            {
                ConfigPath = configPath,
                Concurrency = 8,
                FileSystem = fileSystem,
            };
            var thumbnailGenerator = await ThumbnailGenerator.Factory.CreateAsync(thumbnailGeneratorOptions);

            this.SetSearchControlViewModel(new SearchControlViewModel(fileSystem, thumbnailGenerator));
        }

        public void SetSearchControlViewModel(SearchControlViewModel viewModel)
        {
            var searchControl = this.FindControl<SearchControl>("SearchControl");
            searchControl.ViewModel = viewModel;
        }
    }
}
