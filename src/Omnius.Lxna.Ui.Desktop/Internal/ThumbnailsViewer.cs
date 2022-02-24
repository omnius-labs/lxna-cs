using System.Collections.Immutable;
using Omnius.Core;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;
using Omnius.Lxna.Components.Thumbnail.Models;
using Omnius.Lxna.Ui.Desktop.Internal.Models;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Internal;

public interface IThumbnailsViewer : IAsyncDisposable
{
    void ItemPrepared(IThumbnail thumbnail);

    void ItemClearing(IThumbnail thumbnail);

    ValueTask<IEnumerable<IThumbnail>> StartAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, CancellationToken cancellationToken = default);
}

public class ThumbnailsViewer : AsyncDisposableBase, IThumbnailsViewer
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IThumbnailGenerator _thumbnailGenerator;

    private ImmutableArray<IThumbnail> _models = ImmutableArray<IThumbnail>.Empty;
    private ImmutableHashSet<IThumbnail> _shownModelSet = ImmutableHashSet<IThumbnail>.Empty;

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private ActionPipe _canceledActionPipe = new();

    private readonly AsyncLock _asyncLock = new();

    public ThumbnailsViewer(IThumbnailGenerator thumbnailGenerator)
    {
        _thumbnailGenerator = thumbnailGenerator;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await _task;
    }

    public void ItemPrepared(IThumbnail thumbnail)
    {
        _shownModelSet = _shownModelSet.Add(thumbnail);

        _changedActionPipe.Caller.Call();
    }

    public void ItemClearing(IThumbnail thumbnail)
    {
        _shownModelSet = _shownModelSet.Remove(thumbnail);

        _changedActionPipe.Caller.Call();
    }

    public async ValueTask<IEnumerable<IThumbnail>> StartAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, CancellationToken cancellationToken = default)
    {
        using (await _asyncLock.LockAsync(cancellationToken))
        {
            _canceledActionPipe.Caller.Call();

            await _task;

            var models = ImmutableArray.CreateBuilder<IThumbnail>();

            await foreach (var file in directory.FindFilesAsync(cancellationToken))
            {
                models.Add(new FileThumbnail(file));
            }

            _models = models.ToImmutable();

            var loadTask = this.LoadAsync(thumbnailWidth, thumbnailHeight);
            var rotateTask = this.RotateAsync();
            _task = Task.WhenAll(loadTask, rotateTask);

            return _models;
        }
    }

    private async Task LoadAsync(int width, int height)
    {
        try
        {
            await Task.Delay(1).ConfigureAwait(false);

            using var canceledTokenSource = new CancellationTokenSource();
            using var canceledActionListener = _canceledActionPipe.Listener.Listen(() => canceledTokenSource.Cancel());

            using var changedEvent = new AutoResetEvent(true);
            using var changedActionListener1 = _changedActionPipe.Listener.Listen(() => changedEvent.Set());

            for (; ; )
            {
                await changedEvent.WaitAsync(canceledTokenSource.Token);

                var shownModelSet = new HashSet<IThumbnail>(this.GetShownModels());
                var hiddenModels = _models.Where(n => !shownModelSet.Contains(n)).ToArray();

                using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
                using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => changedTokenSource.Cancel());

                try
                {
                    await this.ClearThumbnailAsync(hiddenModels, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownModelSet.Where(n => n.Thumbnail == null), width, height, false, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownModelSet.Where(n => n.Thumbnail == null), width, height, true, changedTokenSource.Token);
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

    private async Task ClearThumbnailAsync(IEnumerable<IThumbnail> models, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await model.ClearThumbnailAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task LoadThumbnailAsync(IEnumerable<IThumbnail> models, int width, int height, bool cacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new ThumbnailGeneratorGetThumbnailOptions(width, height, ThumbnailFormatType.Png, ThumbnailResizeType.Pad, TimeSpan.FromSeconds(5), 30);

            if (model is FileThumbnail fileThumbnail)
            {
                var result = await _thumbnailGenerator.GetThumbnailAsync(fileThumbnail.File, options, cacheOnly, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorGetThumbnailResultStatus.Succeeded)
                {
                    await model.SetThumbnailAsync(result.Contents).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task RotateAsync()
    {
        try
        {
            await Task.Delay(1).ConfigureAwait(false);

            using var canceledTokenSource = new CancellationTokenSource();
            using var canceledActionListener = _canceledActionPipe.Listener.Listen(() => canceledTokenSource.Cancel());

            for (; ; )
            {
                await Task.Delay(1000, canceledTokenSource.Token).ConfigureAwait(false);

                var models = this.GetShownModels().Where(n => n.IsRotatableThumbnail).ToArray();
                await models.ForEachAsync(async (model) => await model.TryRotateThumbnailAsync(), 128, canceledTokenSource.Token);
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

    private IThumbnail[] GetShownModels()
    {
        var models = _models;
        var shownModelSet = _shownModelSet;

        int minIndex = models.Length;
        int maxIndex = 0;

        foreach (var (model, index) in models.Select((n, i) => (n, i)))
        {
            if (!shownModelSet.Contains(model)) continue;

            minIndex = Math.Min(minIndex, index);
            maxIndex = Math.Max(maxIndex, index);
        }

        minIndex = Math.Max(minIndex - 1, 0);
        maxIndex = Math.Min(maxIndex + 1, models.Length);

        var result = new List<IThumbnail>();

        foreach (var (model, index) in models.Select((n, i) => (n, i)))
        {
            if (index < minIndex || index > maxIndex) continue;

            result.Add(model);
        }

        return result.OrderBy(n => !shownModelSet.Contains(n)).ToArray();
    }
}
