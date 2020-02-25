using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Lxna.Gui.Desktop.Core.Contents;
using Lxna.Gui.Desktop.Models;
using Omnius.Core;
using Omnius.Core.Network;
using Omnius.Lxna.Service;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main
{
    sealed class MainViewModel : AsyncDisposableBase
    {
        private readonly IThumbnailGenerator _thumbnailGenerator;

        private readonly Task _watchTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ObservableCollection<DirectoryModel> _rootDirectoryModels = new ObservableCollection<DirectoryModel>();
        private readonly ObservableCollection<FileModel> _currentFileModels = new ObservableCollection<FileModel>();

        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public MainViewModel(IThumbnailGenerator thumbnailGenerator)
        {
            _thumbnailGenerator = thumbnailGenerator;

            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(null, n)).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryViewModel>().AddTo(_disposable);
            this.SelectedDirectory.Subscribe(n => { if (n != null) { this.TreeView_SelectionChanged(n); } }).AddTo(_disposable);
            this.CurrentFiles = _currentFileModels.ToReadOnlyReactiveCollection(n => new FileViewModel(n)).AddTo(_disposable);

            _watchTask = new Task(this.WatchThread);
            _watchTask.Start();

            // 
            {
                OmniPath.Windows.TryEncoding(@"C:\home\imgs\sample", out var omniPath);
                var model = new DirectoryModel(omniPath);
                _rootDirectoryModels.Add(model);
            }
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _cancellationTokenSource.Cancel();

            await _watchTask;

            _disposable.Dispose();
        }

        public ReadOnlyReactiveCollection<DirectoryViewModel> RootDirectories { get; }
        public ReactiveProperty<DirectoryViewModel> SelectedDirectory { get; }
        public ReadOnlyReactiveCollection<FileViewModel> CurrentFiles { get; }

        private async void WatchThread()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var list = new List<FileViewModel>();
                    list.AddRange(this.CurrentFiles);

                    foreach (var viewModel in list)
                    {
                        OmniPath.Windows.TryEncoding(viewModel.Model.Path, out var omniPath);
                        var result = await _thumbnailGenerator.GetAsync(omniPath, 256, 256, ThumbnailFormatType.Png, ThumbnailResizeType.Crop);

                        var image = result.Contents.FirstOrDefault()?.Image;
                        if (image == null) continue;

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                memoryStream.Write(image.Value.Span);
                                memoryStream.Seek(0, SeekOrigin.Begin);

                                var bitmap = new Bitmap(memoryStream);
                                viewModel.Model.Thumbnail = bitmap;
                            }
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        private void TreeView_SelectionChanged(DirectoryViewModel selectedDirectory)
        {
            this.RefreshTree(selectedDirectory);
        }

        private void RefreshTree(DirectoryViewModel selectedDirectory)
        {
            selectedDirectory.Model.Children.Clear();
            _currentFileModels.Clear();

            OmniPath.Windows.TryDecoding(selectedDirectory.Model.Path, out var path);
            foreach (var filePath in Directory.EnumerateFiles(path))
            {
                _currentFileModels.Add(new FileModel(filePath));
            }
        }
    }
}
