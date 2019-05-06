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

namespace Lxna.Gui.Desktop.Windows
{
    sealed class MainWindowViewModel : DisposableBase
    {
        private LxnaService _lxnaService;

        private ObservableCollection<DirectoryModel> _rootDirectoryModels = new ObservableCollection<DirectoryModel>();
        private ObservableCollection<FileModel> _currentFileModels = new ObservableCollection<FileModel>();

        private CompositeDisposable _disposable = new CompositeDisposable();
        private volatile bool _disposed;

        public MainWindowViewModel()
        {
            _lxnaService = new LxnaService(this.GetLxnaOptions());
            _lxnaService.Load();

            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(n)).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryViewModel>().AddTo(_disposable);
            this.SelectedDirectory.Subscribe(n => { if (n != null) this.TreeView_SelectionChanged(n); }).AddTo(_disposable);

            this.CurrentFiles = _currentFileModels.ToReadOnlyReactiveCollection(n => new FileViewModel(n)).AddTo(_disposable);

            foreach (var contentMetadata in _lxnaService.GetContentIds(null))
            {
                _rootDirectoryModels.Add(new DirectoryModel(contentMetadata));
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
            selectedDirectory.Model.Children.Clear();
            _currentFileModels.Clear();

            foreach (var contentMetadata in _lxnaService.GetContentIds(selectedDirectory.Model.ContentId.Path))
            {
                if (contentMetadata.Type == ContentType.Directory || contentMetadata.Type == ContentType.Archive)
                {
                    selectedDirectory.Model.Children.Add(new DirectoryModel(contentMetadata));
                }
                else if (contentMetadata.Type == ContentType.File)
                {
                    using var image = _lxnaService.GetThumbnails(contentMetadata.Path, 256, 256, ThumbnailFormatType.Png, ThumbnailResizeType.Crop).FirstOrDefault();
                    if (image == null) continue;

                    var fileModel = new FileModel(contentMetadata);

                    var memoryStream = new MemoryStream();
                    {
                        memoryStream.Write(image.Value.Span);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        fileModel.Thumbnail = new Bitmap(memoryStream);
                    }

                    _currentFileModels.Add(fileModel);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _disposable.Dispose();
            }
        }
    }
}
