using Omnius.Core;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Models;

namespace Omnius.Lxna.Ui.Desktop.Interactors;

public interface IThumbnailLoader : IAsyncDisposable
{
    void NotifyItemClearing(ItemViewModel model);

    void NotifyItemPrepared(ItemViewModel model);

    ValueTask StartAsync(int width, int height, IEnumerable<ItemViewModel> itemModels);

    ValueTask StopAsync();
}

public class ThumbnailLoader : AsyncDisposableBase, IThumbnailLoader
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IThumbnailGenerator _thumbnailGenerator;

    private readonly List<ItemViewModel> _itemModels = new();
    private readonly HashSet<ItemViewModel> _shownItemModelSet = new();

    private ValueTask _task = ValueTask.CompletedTask;
    private CancellationTokenSource? _cancellationTokenSource;

    private readonly CallbackManager _callbackManager = new();
    private readonly AutoResetEvent _resetEvent = new(false);

    private readonly object _lockObject = new();

    public ThumbnailLoader(IThumbnailGenerator thumbnailGenerator)
    {
        _thumbnailGenerator = thumbnailGenerator;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _cancellationTokenSource?.Cancel();
        await _task;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public async ValueTask StartAsync(int width, int height, IEnumerable<ItemViewModel> itemModels)
    {
        lock (_lockObject)
        {
            _itemModels.AddRange(itemModels);
        }

        _resetEvent.Set();

        _cancellationTokenSource = new CancellationTokenSource();
        var loadTask = this.LoadAsync(width, height, _cancellationTokenSource.Token);
        var rotateTask = this.RotateAsync(_cancellationTokenSource.Token);
        _task = new ValueTask(Task.WhenAll(loadTask, rotateTask));
    }

    public async ValueTask StopAsync()
    {
        _cancellationTokenSource?.Cancel();
        await _task;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        lock (_lockObject)
        {
            _shownItemModelSet.Clear();
            _itemModels.Clear();
        }
    }

    public void NotifyItemPrepared(ItemViewModel model)
    {
        lock (_lockObject)
        {
            _shownItemModelSet.Add(model);
        }

        _resetEvent.Set();
        _callbackManager.Invoke();
    }

    public void NotifyItemClearing(ItemViewModel model)
    {
        lock (_lockObject)
        {
            _shownItemModelSet.Remove(model);
        }

        _resetEvent.Set();
        _callbackManager.Invoke();
    }

    private async Task LoadAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                await _resetEvent.WaitAsync(cancellationToken);

                var shownItemModels = new List<ItemViewModel>(this.GetShownItemModels());
                var hiddenItemModels = new List<ItemViewModel>(this.GetHiddenItemModels());

                using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using var unsubscriber = _callbackManager.Subscribe(() => linkedCancellationTokenSource.Cancel());

                try
                {
                    await this.ClearThumbnailAsync(hiddenItemModels, linkedCancellationTokenSource.Token);
                    await this.LoadThumbnailAsync(shownItemModels.Where(n => n.Thumbnail == null), width, height, false, linkedCancellationTokenSource.Token);
                    await this.LoadThumbnailAsync(shownItemModels.Where(n => n.Thumbnail == null), width, height, true, linkedCancellationTokenSource.Token);
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

    private async Task ClearThumbnailAsync(IEnumerable<ItemViewModel> targetModels, CancellationToken cancellationToken)
    {
        foreach (var model in targetModels)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await model.ClearThumbnailAsync().ConfigureAwait(false);
        }
    }

    private async Task LoadThumbnailAsync(IEnumerable<ItemViewModel> targetModels, int width, int height, bool cacheOnly, CancellationToken cancellationToken)
    {
        foreach (var model in targetModels)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new ThumbnailGeneratorGetThumbnailOptions(width, height, ThumbnailFormatType.Png, ThumbnailResizeType.Pad, TimeSpan.FromSeconds(5), 30);
            var result = await _thumbnailGenerator.GetThumbnailAsync(model.Path, options, cacheOnly, cancellationToken).ConfigureAwait(false);

            if (result.Status == ThumbnailGeneratorGetThumbnailResultStatus.Succeeded)
            {
                await model.SetThumbnailAsync(result.Contents).ConfigureAwait(false);
            }
        }
    }

    private async Task RotateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                var targetModels = new List<ItemViewModel>(this.GetShownItemModels());
                await targetModels.ForEachAsync(async (model) => await model.RotateThumbnailAsync(), 128, cancellationToken);
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

    private IEnumerable<ItemViewModel> GetHiddenItemModels()
    {
        var itemModels = new List<ItemViewModel>();

        lock (_lockObject)
        {
            itemModels.AddRange(_itemModels);
        }

        var shownItemModels = new List<ItemViewModel>(this.GetShownItemModels());
        var shownItemModelSet = new HashSet<ItemViewModel>(shownItemModels);

        return itemModels.Where(n => !shownItemModelSet.Contains(n)).ToArray();
    }

    private IEnumerable<ItemViewModel> GetShownItemModels()
    {
        var shownSet = new HashSet<ItemViewModel>();
        var itemModels = new List<ItemViewModel>();

        lock (_lockObject)
        {
            itemModels.AddRange(_itemModels);
            shownSet.UnionWith(_shownItemModelSet);
        }

        int minIndex = itemModels.Count;
        int maxIndex = 0;

        foreach (var (model, index) in itemModels.Select((n, i) => (n, i)))
        {
            if (!shownSet.Contains(model)) continue;

            minIndex = Math.Min(minIndex, index);
            maxIndex = Math.Max(maxIndex, index);
        }

        minIndex = Math.Max(minIndex - 1, 0);
        maxIndex = Math.Min(maxIndex + 1, itemModels.Count);

        var result = new List<ItemViewModel>();

        foreach (var (model, index) in itemModels.Select((n, i) => (n, i)))
        {
            if (index < minIndex || index > maxIndex) continue;

            result.Add(model);
        }

        return result.OrderBy(n => !shownSet.Contains(n)).ToArray();
    }

    private sealed class CallbackManager
    {
        private event Action? Event;

        public void Invoke()
        {
            this.Event?.Invoke();
        }

        public IDisposable Subscribe(Action action)
        {
            this.Event += action;
            return new Cookie(this, action);
        }

        private void Unsubscribe(Action action)
        {
            this.Event -= action;
        }

        private sealed class Cookie : IDisposable
        {
            private readonly CallbackManager _callbackManager;
            private readonly Action _action;

            public Cookie(CallbackManager callbackManager, Action action)
            {
                _callbackManager = callbackManager;
                _action = action;
            }

            public void Dispose()
            {
                _callbackManager.Unsubscribe(_action);
            }
        }
    }
}
