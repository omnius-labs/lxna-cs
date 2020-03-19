using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Extensions;
using Omnius.Lxna.Service;
using Omnius.Lxna.Ui.Desktop.Engine.Models;

namespace Omnius.Lxna.Ui.Desktop.Engine
{
    class ThumbnailLoader : IAsyncDisposable
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IThumbnailGenerator _thumbnailGenerator;

        private readonly List<ItemModel> _itemModels = new List<ItemModel>();

        private readonly HashSet<ItemModel> _shownModelSet = new HashSet<ItemModel>();
        private readonly object _shownModelSetLockObject = new object();

        private readonly EventManager _shownModelSetChangedEventManager = new EventManager();
        private readonly AutoResetEvent _shownModelSetChangedEvent = new AutoResetEvent(false);

        private readonly Task _loadTask;
        private readonly Task _rotateTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public ThumbnailLoader(IThumbnailGenerator thumbnailGenerator, IEnumerable<ItemModel> itemModels)
        {
            _thumbnailGenerator = thumbnailGenerator;
            _itemModels.AddRange(itemModels);

            _loadTask = this.LoadAsync(_cancellationTokenSource.Token);
            _rotateTask = this.RotateAsync(_cancellationTokenSource.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();

            await _loadTask.ConfigureAwait(false);
            await _rotateTask.ConfigureAwait(false);

            _cancellationTokenSource.Dispose();
        }

        private IEnumerable<ItemModel> GetLoadTargetItemModels()
        {
            lock (_shownModelSetLockObject)
            {
                int minIndex = _itemModels.Count;
                int maxIndex = 0;

                foreach (var (model, index) in _itemModels.Select((n, i) => (n, i)))
                {
                    if (!_shownModelSet.Contains(model)) continue;

                    minIndex = Math.Min(minIndex, index);
                    maxIndex = Math.Max(maxIndex, index);
                }

                minIndex = Math.Max(minIndex - 10, 0);
                maxIndex = Math.Min(maxIndex + 10, _itemModels.Count);

                var result = new List<ItemModel>();

                foreach (var (model, index) in _itemModels.Select((n, i) => (n, i)))
                {
                    if (index < minIndex || index > maxIndex) continue;
                    result.Add(model);
                }

                return result.OrderBy(n => !_shownModelSet.Contains(n)).ToArray();
            }
        }

        private async Task LoadAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1).ConfigureAwait(false);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _shownModelSetChangedEvent.WaitAsync(cancellationToken);

                    var targetModels = new List<ItemModel>(this.GetLoadTargetItemModels());
                    var targetModelSet = new HashSet<ItemModel>(targetModels);

                    // Clear
                    foreach (var model in _itemModels.Where(n => n.Thumbnail != null))
                    {
                        if (targetModelSet.Contains(model)) continue;

                        await model.ClearThumbnailAsync().ConfigureAwait(false);
                    }

                    // Load
                    try
                    {
                        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                        foreach (var model in targetModels.Where(n => n.Thumbnail == null))
                        {
                            using var cookie = _shownModelSetChangedEventManager.Subscribe(() =>
                            {
                                linkedCancellationTokenSource.Cancel();
                            });

                            var options = new ThumbnailGeneratorGetThumbnailOptions(256, 256, ThumbnailFormatType.Png, ThumbnailResizeType.Pad, TimeSpan.FromSeconds(5), 30);
                            var result = await _thumbnailGenerator.GetThumbnailAsync(model.Path, options, true, cancellationToken).ConfigureAwait(false);

                            if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                            {
                                await model.SetThumbnailAsync(result.Contents).ConfigureAwait(false);
                            }
                        }

                        bool abort = false;

                        foreach (var model in targetModels.Where(n => n.Thumbnail == null))
                        {
                            using var cookie = _shownModelSetChangedEventManager.Subscribe(() =>
                            {
                                var targetModelSet = new HashSet<ItemModel>(this.GetLoadTargetItemModels());

                                if (!targetModelSet.Contains(model))
                                {
                                    linkedCancellationTokenSource.Cancel();
                                }

                                abort = true;
                            });

                            var options = new ThumbnailGeneratorGetThumbnailOptions(256, 256, ThumbnailFormatType.Png, ThumbnailResizeType.Pad, TimeSpan.FromSeconds(5), 30);
                            var result = await _thumbnailGenerator.GetThumbnailAsync(model.Path, options, false, cancellationToken).ConfigureAwait(false);

                            if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                            {
                                await model.SetThumbnailAsync(result.Contents).ConfigureAwait(false);
                            }

                            if (abort) break;
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        _logger.Debug(e);
                    }
                }
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

        private async Task RotateAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1).ConfigureAwait(false);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                    var targetModels = new List<ItemModel>(this.GetLoadTargetItemModels());

                    foreach (var model in targetModels)
                    {
                        await model.RotateThumbnailAsync().ConfigureAwait(false);
                    }
                }
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

        public void NotifyItemPrepared(ItemModel model)
        {
            lock (_shownModelSetLockObject)
            {
                _shownModelSet.Add(model);
            }

            _shownModelSetChangedEvent.Set();
            _shownModelSetChangedEventManager.Invoke();
        }

        public void NotifyItemClearing(ItemModel model)
        {
            lock (_shownModelSetLockObject)
            {
                _shownModelSet.Remove(model);
            }

            _shownModelSetChangedEvent.Set();
            _shownModelSetChangedEventManager.Invoke();
        }

        private sealed class EventManager
        {
            private event Action? _event;

            public void Invoke()
            {
                _event?.Invoke();
            }

            public IDisposable Subscribe(Action action)
            {
                _event += action;
                return new Cookie(this, action);
            }

            private void Unsubscribe(Action action)
            {
                _event -= action;
            }

            private sealed class Cookie : IDisposable
            {
                private readonly EventManager _eventManager;
                private readonly Action _action;

                public Cookie(EventManager eventManager, Action action)
                {
                    _eventManager = eventManager;
                    _action = action;
                }

                public void Dispose()
                {
                    _eventManager.Unsubscribe(_action);
                }
            }
        }
    }
}
