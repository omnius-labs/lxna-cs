using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Configuration;
using Omnius.Lxna.Ui.Desktop.Helpers;
using Omnius.Lxna.Ui.Desktop.Interactors;
using Omnius.Lxna.Ui.Desktop.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.Windows.Main.FileView
{
    public class FileViewControlViewModel : AsyncDisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly UiState _uiState;
        private readonly IFileSystem _fileSystem;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly IDialogService _dialogService;

        private readonly ThumbnailLoader _thumbnailLoader;

        private TaskState? _refreshCurrentItemModelsTaskState = null;
        private readonly AsyncLock _refreshCurrentItemModelsTaskAsyncLock = new();

        private readonly ObservableCollection<DirectoryModel> _rootDirectoryModels = new();
        private readonly ObservableCollection<ItemModel> _currentItemModels = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CompositeDisposable _disposable = new();

        public FileViewControlViewModel(UiState uiState, IFileSystem fileSystem, IThumbnailGenerator thumbnailGenerator, IDialogService dialogService)
        {
            _uiState = uiState;
            _fileSystem = fileSystem;
            _thumbnailGenerator = thumbnailGenerator;
            _dialogService = dialogService;

            _thumbnailLoader = new ThumbnailLoader(thumbnailGenerator);

            this.TreeViewWidth = _uiState.ToReactivePropertySlimAsSynchronized(n => n.FileView_TreeViewWidth, convert: ConvertHelper.DoubleToGridLength, convertBack: ConvertHelper.GridLengthToDouble).AddTo(_disposable);
            this.Thumbnail_Width = _uiState.ToReactivePropertySlimAsSynchronized(n => n.Thumbnail_Width).AddTo(_disposable);
            this.Thumbnail_Height = _uiState.ToReactivePropertySlimAsSynchronized(n => n.Thumbnail_Height).AddTo(_disposable);
            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryModel>().AddTo(_disposable);
            this.SelectedDirectory.Where(n => n is not null).Subscribe(n => this.TreeView_SelectionChanged(n)).AddTo(_disposable);
            this.CurrentItems = _currentItemModels.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);

            this.Init();
        }

        private async void Init()
        {
            foreach (var drive in await _fileSystem.FindDirectoriesAsync())
            {
                var model = new DirectoryModel(drive, _fileSystem);
                _rootDirectoryModels.Add(model);
            }
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _disposable.Dispose();
            _cancellationTokenSource.Cancel();

            using (await _refreshCurrentItemModelsTaskAsyncLock.LockAsync())
            {
                if (_refreshCurrentItemModelsTaskState is not null)
                {
                    _refreshCurrentItemModelsTaskState.CancellationTokenSource.Cancel();
                    await _refreshCurrentItemModelsTaskState.Task;
                    _refreshCurrentItemModelsTaskState.CancellationTokenSource.Dispose();
                }
            }

            await _thumbnailLoader.DisposeAsync();

            _cancellationTokenSource.Dispose();
        }

        public ReactivePropertySlim<GridLength> TreeViewWidth { get; }

        public ReactivePropertySlim<int> Thumbnail_Width { get; }

        public ReactivePropertySlim<int> Thumbnail_Height { get; }

        public ReadOnlyReactiveCollection<DirectoryModel> RootDirectories { get; }

        public ReactiveProperty<DirectoryModel> SelectedDirectory { get; }

        public ReadOnlyReactiveCollection<ItemModel> CurrentItems { get; }

        public async void NotifyDoubleTapped(object item)
        {
            if (item is not ItemModel model) return;

            var path = model.Path;

            if (await _fileSystem.ExistsDirectoryAsync(path))
            {
                var directoryViewModel = this.SelectedDirectory.Value.Children.FirstOrDefault(n => n.Path == path);
                if (directoryViewModel is null) return;

                this.SelectedDirectory.Value.IsExpanded = true;
                this.SelectedDirectory.Value = directoryViewModel;
            }
            else if (await _fileSystem.ExistsFileAsync(path))
            {
                await _dialogService.OpenPicturePreviewWindowAsync(path);
            }
        }

        public void NotifyItemPrepared(object item)
        {
            if (item is ItemModel model)
            {
                _thumbnailLoader.NotifyItemPrepared(model);
            }
        }

        public void NotifyItemClearing(object item)
        {
            if (item is ItemModel model)
            {
                _thumbnailLoader.NotifyItemClearing(model);
            }
        }

        private async void TreeView_SelectionChanged(DirectoryModel selectedDirectory)
        {
            await this.RefreshAsync(selectedDirectory);
        }

        private async Task RefreshAsync(DirectoryModel selectedDirectory)
        {
            await Task.Delay(1).ConfigureAwait(false);

            using (await _refreshCurrentItemModelsTaskAsyncLock.LockAsync())
            {
                // 古い再描画タスクを終了
                if (_refreshCurrentItemModelsTaskState is not null)
                {
                    _refreshCurrentItemModelsTaskState.CancellationTokenSource.Cancel();
                    await _refreshCurrentItemModelsTaskState.Task;
                    _refreshCurrentItemModelsTaskState.CancellationTokenSource.Dispose();
                }

                // 新しい描画タスクを開始
                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
                var task = this.RefreshCurrentItemModelsAsync(selectedDirectory.Path, cancellationTokenSource.Token);
                _refreshCurrentItemModelsTaskState = new TaskState(task, cancellationTokenSource);
            }
        }

        private async Task RefreshCurrentItemModelsAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);

                await _thumbnailLoader.StopAsync();

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var model in _currentItemModels.ToArray())
                    {
                        model.Dispose();
                    }

                    _currentItemModels.Clear();
                });

                var files = new List<NestedPath>();
                var dirs = new List<NestedPath>();

                try
                {
                    files.AddRange(await _fileSystem.FindFilesAsync(path, cancellationToken));
                    dirs.AddRange(await _fileSystem.FindDirectoriesAsync(path, cancellationToken));
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.Error(e);
                }

                files.Sort();
                dirs.Sort();

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var dirPath in dirs)
                    {
                        _currentItemModels.Add(new ItemModel(dirPath));
                    }

                    foreach (var filePath in files)
                    {
                        _currentItemModels.Add(new ItemModel(filePath));
                    }
                });

                await _thumbnailLoader.StartAsync(256, 256, _currentItemModels);
            }
            catch (OperationCanceledException e)
            {
                _logger.Debug(e);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        private class TaskState
        {
            public TaskState(Task task, CancellationTokenSource cancellationTokenSource)
            {
                this.Task = task;
                this.CancellationTokenSource = cancellationTokenSource;
            }

            public Task Task { get; }

            public CancellationTokenSource CancellationTokenSource { get; }
        }
    }
}
