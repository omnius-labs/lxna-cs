using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using Avalonia.Media.Imaging;
using Lxna.Core;
using Lxna.Messages;
using Omnix.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Lxna.Gui.Desktop.Models;
using Lxna.Gui.Desktop.Base.Contents;
using System.Threading.Channels;
using System.Threading;

namespace Lxna.Gui.Desktop.Windows.Main
{
    sealed class MainWindowViewModel : DisposableBase
    {
        private readonly LxnaService _lxnaService;

        private readonly Channel<FileViewModel> _fileViewModelChannel;

        private TaskManager _watchTask;

        private readonly ObservableCollection<DirectoryModel> _rootDirectoryModels = new ObservableCollection<DirectoryModel>();
        private readonly ObservableCollection<FileModel> _currentFileModels = new ObservableCollection<FileModel>();

        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            _lxnaService = new LxnaService(this.GetLxnaOptions());
            _lxnaService.Load();

            _fileViewModelChannel = Channel.CreateUnbounded<FileViewModel>();

            _watchTask = new TaskManager(this.WatchThread);
            _watchTask.Start();

            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(n)).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryViewModel>().AddTo(_disposable);
            this.SelectedDirectory.Subscribe(n => { if (n != null) { this.TreeView_SelectionChanged(n); } }).AddTo(_disposable);
            this.CurrentFiles = _currentFileModels.ToReadOnlyReactiveCollection(n => new FileViewModel(n, (viewModel) => _fileViewModelChannel.Writer.TryWrite(viewModel))).AddTo(_disposable);

            foreach (var contentMetadata in _lxnaService.GetContentIds(null))
            {
                var model = new DirectoryModel(contentMetadata);
                this.RefreshTree(model);
                _rootDirectoryModels.Add(model);
            }
        }

        private async void WatchThread(CancellationToken token)
        {
            try
            {
                /*
                while (!token.IsCancellationRequested)
                {
                    if (!_fileViewModelChannel.Reader.TryRead(out var viewModel))
                    {
                        await _fileViewModelChannel.Reader.WaitToReadAsync(token);
                        continue;
                    }

                    using var image = _lxnaService.GetThumbnails(viewModel.Model.ContentId.Path, 256, 256, LxnaThumbnailFormatType.Png, LxnaThumbnailResizeType.Pad).FirstOrDefault();
                    if (image == null)
                    {
                        continue;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(image.Value.Span);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        var bitmap = new Bitmap(memoryStream);
                        viewModel.SetThumbnail(bitmap);
                    }
                }
                */
            }
            catch (OperationCanceledException)
            {

            }
        }

        public ReadOnlyReactiveCollection<DirectoryViewModel> RootDirectories { get; }
        public ReactiveProperty<DirectoryViewModel> SelectedDirectory { get; }
        public ReadOnlyReactiveCollection<FileViewModel> CurrentFiles { get; }

        private LxnaOptions GetLxnaOptions()
        {
            string directoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            directoryPath = Path.GetDirectoryName(directoryPath);
            directoryPath = Path.Combine(directoryPath, "config");

            return new LxnaOptions(directoryPath);
        }

        private void TreeView_SelectionChanged(DirectoryViewModel selectedDirectory)
        {
            foreach (var viewModel in selectedDirectory.Children)
            {
                this.RefreshTree(viewModel.Model);
            }
        }

        private void RefreshTree(DirectoryModel selectedDirectory)
        {
            selectedDirectory.Children.Clear();
            _currentFileModels.Clear();

            foreach (var contentId in _lxnaService.GetContentIds(selectedDirectory.ContentId.Address))
            {
                if (contentId.Type == LxnaContentType.Directory || contentId.Type == LxnaContentType.Archive)
                {
                    selectedDirectory.Children.Add(new DirectoryModel(contentId));
                }
                else if (contentId.Type == LxnaContentType.File)
                {
                    _currentFileModels.Add(new FileModel(contentId));
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                _watchTask.Cancel();
                _watchTask.Dispose();

                _disposable.Dispose();
            }
        }
    }
}
