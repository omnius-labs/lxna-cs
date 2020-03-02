using System;
using System.Collections.Concurrent;
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

namespace Omnius.Lxna.Ui.Desktop.Views.Main
{
    public sealed class MainViewModel : AsyncDisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IThumbnailGenerator _thumbnailGenerator;

        private readonly Dictionary<object, int> _thumbnailLoadRequests = new Dictionary<object, int>();
        private readonly object _thumbnailLoadRequestsLockObject = new object();

        private readonly Task _loadTask;
        private readonly Task _rotateTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ObservableCollection<DirectoryModel> _rootDirectoryModels = new ObservableCollection<DirectoryModel>();
        private readonly ObservableCollection<FileModel> _currentFileModels = new ObservableCollection<FileModel>();

        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public MainViewModel()
        {

        }

        public MainViewModel(IThumbnailGenerator thumbnailGenerator)
        {
            _thumbnailGenerator = thumbnailGenerator;

            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(null, n)).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryViewModel>().AddTo(_disposable);
            this.SelectedDirectory.Subscribe(n => { if (n != null) { this.TreeView_SelectionChanged(n); } }).AddTo(_disposable);
            this.CurrentItems = _currentFileModels.ToReadOnlyReactiveCollection(n => new FileViewModel(n)).AddTo(_disposable);

            _loadTask = new Task(this.LoadThread);
            _loadTask.Start();

            _rotateTask= new Task(this.RotateThread);
            _rotateTask.Start();

            // FIXME
            foreach (var drive in Directory.GetLogicalDrives())
            {
                if (!OmniPath.Windows.TryEncoding(drive, out var omniPath))
                {
                    continue;
                }

                var model = new DirectoryModel(omniPath);
                _rootDirectoryModels.Add(model);
            }
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _cancellationTokenSource.Cancel();

            await _loadTask;
            await _rotateTask;

            _disposable.Dispose();
        }

        public ReadOnlyReactiveCollection<DirectoryViewModel> RootDirectories { get; }
        public ReactiveProperty<DirectoryViewModel> SelectedDirectory { get; }
        public ReadOnlyReactiveCollection<FileViewModel> CurrentItems { get; }

        public void NotifyItemPrepared(object item, int index)
        {
            lock (_thumbnailLoadRequestsLockObject)
            {
                _thumbnailLoadRequests.Add(item, index);
            }
        }

        internal void NotifyItemIndexChanged(object item, int oldIndex, int newIndex)
        {
            lock (_thumbnailLoadRequestsLockObject)
            {
                _thumbnailLoadRequests[item] = newIndex;
            }
        }

        public void NotifyItemClearing(object item)
        {
            lock (_thumbnailLoadRequestsLockObject)
            {
                _thumbnailLoadRequests.Remove(item);
            }
        }

        private async void LoadThread()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);

                    FileViewModel fileViewModel = null;

                    lock (_thumbnailLoadRequestsLockObject)
                    {
                        var tempList = _thumbnailLoadRequests.ToList();
                        tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                        fileViewModel = tempList
                            .Select(n => n.Key)
                            .OfType<FileViewModel>()
                            .Where(n => n.Thumbnail.Value == null)
                            .FirstOrDefault();
                    }

                    if (fileViewModel != null)
                    {
                        if (!OmniPath.Windows.TryEncoding(fileViewModel.Model.Path, out var omniPath))
                        {
                            continue;
                        }

                        var result = await _thumbnailGenerator.GetThumbnailAsync(omniPath, 256, 256, ThumbnailFormatType.Png, ThumbnailResizeType.Pad);

                        await fileViewModel.Model.SetThumbnailsAsync(result.Contents);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e) 
            {
                _logger.Debug(e);
                throw e;
            }
        }

        private async void RotateThread()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token);

                    FileViewModel[] viewModels;

                    lock (_thumbnailLoadRequestsLockObject)
                    {
                        var tempList = _thumbnailLoadRequests.ToList();
                        tempList.Sort((x, y) => x.Value.CompareTo(y.Value));
                        viewModels = tempList
                            .Select(n => n.Key)
                            .OfType<FileViewModel>()
                            .Where(n=>n.Thumbnail.Value != null)
                            .ToArray();
                    }

                    foreach(var viewModel in viewModels)
                    {
                        await viewModel.Model.RotateThumbnailAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                _logger.Debug(e);
                throw e;
            }
        }

        private void TreeView_SelectionChanged(DirectoryViewModel selectedDirectory)
        {
            this.RefreshTree(selectedDirectory);
        }

        private void RefreshTree(DirectoryViewModel selectedDirectory)
        {
            _currentFileModels.Clear();

            OmniPath.Windows.TryDecoding(selectedDirectory.Model.Path, out var path);
            foreach (var filePath in Directory.EnumerateFiles(path))
            {
                _currentFileModels.Add(new FileModel(filePath));
            }
        }
    }
}
