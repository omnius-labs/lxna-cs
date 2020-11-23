using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Ui.Desktop.Interactors;
using Omnius.Lxna.Ui.Desktop.Interactors.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace Omnius.Lxna.Ui.Desktop.ViewModels
{
    public class SearchControlViewModel : AsyncDisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IThumbnailGenerator _thumbnailGenerator;
        private ThumbnailLoader _thumbnailLoader;

        private readonly ObservableCollection<DirectoryModel> _rootDirectoryModels = new ObservableCollection<DirectoryModel>();
        private readonly ObservableCollection<ItemModel> _currentItemModels = new ObservableCollection<ItemModel>();

        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private readonly AsyncLock _asyncLock = new AsyncLock();

        public SearchControlViewModel(IThumbnailGenerator thumbnailGenerator)
        {
            _thumbnailGenerator = thumbnailGenerator;

            this.RootDirectories = _rootDirectoryModels.ToReadOnlyReactiveCollection(n => new DirectoryViewModel(null, n)).AddTo(_disposable);
            this.SelectedDirectory = new ReactiveProperty<DirectoryViewModel>().AddTo(_disposable);
            this.SelectedDirectory.Subscribe(n => { if (n != null) { this.TreeView_SelectionChanged(n); } }).AddTo(_disposable);
            this.CurrentItems = _currentItemModels.ToReadOnlyReactiveCollection(n => new ItemViewModel(n)).AddTo(_disposable);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var drive in Directory.GetLogicalDrives())
                {
                    var model = new DirectoryModel(drive);
                    _rootDirectoryModels.Add(model);
                }
            }
        }

        protected override async ValueTask OnDisposeAsync()
        {
            using (await _asyncLock.LockAsync())
            {
                if (_thumbnailLoader != null)
                {
                    await _thumbnailLoader.DisposeAsync();
                }

                _disposable.Dispose();
            }
        }

        public ReadOnlyReactiveCollection<DirectoryViewModel> RootDirectories { get; }

        public ReactiveProperty<DirectoryViewModel> SelectedDirectory { get; }

        public ReadOnlyReactiveCollection<ItemViewModel> CurrentItems { get; }

        public void NotifyDoubleTapped(object item)
        {
            var path = ((ItemViewModel)item).Model.Path;

            var process = new Process();
            process.StartInfo.FileName = path;
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        public void NotifyItemPrepared(object item)
        {
            if (item is ItemViewModel viewModel)
            {
                _thumbnailLoader?.NotifyItemPrepared(viewModel.Model);
            }
        }

        public void NotifyItemClearing(object item)
        {
            if (item is ItemViewModel viewModel)
            {
                _thumbnailLoader?.NotifyItemClearing(viewModel.Model);
            }
        }

        private void TreeView_SelectionChanged(DirectoryViewModel selectedDirectory)
        {
            this.RefreshTree(selectedDirectory);
        }

        private async void RefreshTree(DirectoryViewModel selectedDirectory)
        {
            using (await _asyncLock.LockAsync())
            {
                // 古い描画タスクを終了する
                if (_thumbnailLoader != null)
                {
                    await _thumbnailLoader.DisposeAsync();
                }

                try
                {
                    var oldModels = _currentItemModels.ToArray();
                    _currentItemModels.Clear();

                    foreach (var model in oldModels)
                    {
                        model.Dispose();
                    }

                    var tempList = Directory.GetFiles(selectedDirectory.Model.Path).ToList();
                    tempList.Sort();

                    foreach (var filePath in tempList)
                    {
                        _currentItemModels.Add(new ItemModel(filePath));
                    }
                }
                catch (UnauthorizedAccessException)
                {

                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }

                // 新しい描画タスクを開始する
                {
                    _thumbnailLoader = new ThumbnailLoader(_thumbnailGenerator, _currentItemModels);
                }
            }
        }
    }
}
