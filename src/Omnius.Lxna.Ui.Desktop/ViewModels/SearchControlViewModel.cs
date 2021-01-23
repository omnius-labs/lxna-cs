using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Interactors;
using Omnius.Lxna.Ui.Desktop.Interactors.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.ViewModels
{
    public class SearchControlViewModel : AsyncDisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFileSystem _fileSystem;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly ThumbnailLoader _thumbnailLoader;

        private TaskStatus? _refreshCurrentItemModelsTaskStatus = null;
        private readonly AsyncLock _refreshCurrentItemModelsTaskAsyncLock = new AsyncLock();

        private readonly ObservableCollection<DirectoryModel> _rootDirectoryModels = new ObservableCollection<DirectoryModel>();
        private readonly ObservableCollection<ItemModel> _currentItemModels = new ObservableCollection<ItemModel>();

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public SearchControlViewModel(IFileSystem fileSystem, IThumbnailGenerator thumbnailGenerator)
        {
            _fileSystem = fileSystem;
            _thumbnailGenerator = thumbnailGenerator;
            _thumbnailLoader = new ThumbnailLoader(_thumbnailGenerator);

            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => n).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryModel>().AddTo(_disposable);
            this.SelectedDirectory.Subscribe(n =>
            {
                if (n != null)
                {
                    this.TreeView_SelectionChanged(n);
                }
            }).AddTo(_disposable);
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
                if (_refreshCurrentItemModelsTaskStatus is not null)
                {
                    _refreshCurrentItemModelsTaskStatus.CancellationTokenSource.Cancel();
                    await _refreshCurrentItemModelsTaskStatus.Task;
                    _refreshCurrentItemModelsTaskStatus.CancellationTokenSource.Dispose();
                }
            }

            await _thumbnailLoader.DisposeAsync();

            _cancellationTokenSource.Dispose();
        }

        public ReadOnlyReactiveCollection<DirectoryModel> RootDirectories { get; }

        public ReactiveProperty<DirectoryModel> SelectedDirectory { get; }

        public ReadOnlyReactiveCollection<ItemModel> CurrentItems { get; }

        public async void NotifyDoubleTapped(object item)
        {
            var path = ((ItemModel)item).Path;
            if (await _fileSystem.ExistsDirectoryAsync(path))
            {
                var directoryViewModel = this.SelectedDirectory.Value.Children.FirstOrDefault(n => n.Path == path);
                if (directoryViewModel is not null)
                {
                    this.SelectedDirectory.Value.IsExpanded = true;
                    //  this.SelectedDirectory.Value = directoryViewModel;
                }
            }
            else if (await _fileSystem.ExistsFileAsync(path))
            {
                // var process = new Process();
                // process.StartInfo.UseShellExecute = true;
                // process.Start();
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
            await this.RefreshCurrentItemModelsAsync(selectedDirectory);
        }

        private async Task RefreshCurrentItemModelsAsync(DirectoryModel selectedDirectory)
        {
            await Task.Delay(1).ConfigureAwait(false);

            using (await _refreshCurrentItemModelsTaskAsyncLock.LockAsync())
            {
                // 古い再描画タスクを終了する
                if (_refreshCurrentItemModelsTaskStatus is not null)
                {
                    _refreshCurrentItemModelsTaskStatus.CancellationTokenSource.Cancel();
                    await _refreshCurrentItemModelsTaskStatus.Task;
                    _refreshCurrentItemModelsTaskStatus.CancellationTokenSource.Dispose();
                }

                var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
                var task = this.RefreshCurrentItemModelsAsync(selectedDirectory.Path, cancellationTokenSource.Token);

                _refreshCurrentItemModelsTaskStatus = new TaskStatus(task, cancellationTokenSource);
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

                var tempList = new List<NestedPath>();

                try
                {
                    tempList.AddRange(await _fileSystem.FindFilesAsync(path, cancellationToken));
                    tempList.AddRange(await _fileSystem.FindDirectoriesAsync(path, cancellationToken));
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.Error(e);
                }

                tempList.Sort();

                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var filePath in tempList)
                    {
                        _currentItemModels.Add(new ItemModel(filePath));
                    }
                });

                _thumbnailLoader.Start(_currentItemModels);
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

        private class TaskStatus
        {
            public TaskStatus(Task task, CancellationTokenSource cancellationTokenSource)
            {
                this.Task = task;
                this.CancellationTokenSource = cancellationTokenSource;
            }

            public Task Task { get; }

            public CancellationTokenSource CancellationTokenSource { get; }
        }
    }
}
