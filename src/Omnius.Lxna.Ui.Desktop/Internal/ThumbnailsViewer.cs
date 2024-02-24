using System.Collections.Immutable;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Internal;

public record struct ThumbnailsViewerLoadResult
{
    public ImmutableArray<Thumbnail<object>> Thumbnails { get; init; }
}

public class ThumbnailsViewer : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly DirectoryThumbnailGenerator _directoryThumbnailGenerator;
    private readonly FileThumbnailGenerator _fileThumbnailGenerator;
    private readonly IApplicationDispatcher _applicationDispatcher;

    private ImmutableArray<Thumbnail<object>> _thumbnails = ImmutableArray<Thumbnail<object>>.Empty;
    private ImmutableDictionary<Thumbnail<object>, int> _thumbnailIndexMap = ImmutableDictionary<Thumbnail<object>, int>.Empty;
    private ImmutableHashSet<int> _preparedThumbnailIndexSet = ImmutableHashSet<int>.Empty;

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private ActionPipe _canceledActionPipe = new();

    private readonly AsyncLock _asyncLock = new();

    public ThumbnailsViewer(DirectoryThumbnailGenerator directoryThumbnailGenerator, FileThumbnailGenerator fileThumbnailGenerator, IApplicationDispatcher applicationDispatcher)
    {
        _directoryThumbnailGenerator = directoryThumbnailGenerator;
        _fileThumbnailGenerator = fileThumbnailGenerator;
        _applicationDispatcher = applicationDispatcher;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await _task;
    }

    public void ThumbnailPrepared(Thumbnail<object> thumbnail)
    {
        if (_thumbnailIndexMap.TryGetValue(thumbnail, out var index))
        {
            _preparedThumbnailIndexSet = _preparedThumbnailIndexSet.Add(index);
            _changedActionPipe.Caller.Call();
        }
    }

    public void ThumbnailClearing(Thumbnail<object> thumbnail)
    {
        if (_thumbnailIndexMap.TryGetValue(thumbnail, out var index))
        {
            _preparedThumbnailIndexSet = _preparedThumbnailIndexSet.Remove(index);
            _changedActionPipe.Caller.Call();
        }
    }

    public async ValueTask<ThumbnailsViewerLoadResult> LoadAsync(IDirectory directory,
        int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan,
        Comparison<object> comparison, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            _canceledActionPipe.Caller.Call();

            await _task;

            var items = new List<object>();

            foreach (var file in await directory.FindFilesAsync(cancellationToken))
            {
                items.Add(file);
            }

            foreach (var dir in await directory.FindDirectoriesAsync(cancellationToken))
            {
                items.Add(dir);
            }

            items.Sort(comparison.Invoke);

            _thumbnails = items.Select(n => new Thumbnail<object>(n)).ToImmutableArray();

            var thumbnailIndexMap = ImmutableDictionary.CreateBuilder<Thumbnail<object>, int>();

            foreach (var (thumbnail, index) in _thumbnails.Select((n, i) => (n, i)))
            {
                thumbnailIndexMap.Add(thumbnail, index);
            }

            _thumbnailIndexMap = thumbnailIndexMap.ToImmutable();

            var loadTask = this.LoadAsync(thumbnailWidth, thumbnailHeight);
            var rotateTask = this.RotateAsync(rotationSpan);
            _task = Task.WhenAll(loadTask, rotateTask);

            return new ThumbnailsViewerLoadResult { Thumbnails = _thumbnails };
        }
    }

    private async Task LoadAsync(int width, int height)
    {
        try
        {
            await Task.Delay(1).ConfigureAwait(false);

            using var canceledTokenSource = new CancellationTokenSource();
            using var canceledActionListener = _canceledActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => canceledTokenSource.Cancel()));

            using var changedEvent = new AutoResetEvent(true);
            using var changedActionListener1 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedEvent.Set()));

            for (; ; )
            {
                await changedEvent.WaitAsync(canceledTokenSource.Token);

                var shownThumbnailSet = new HashSet<Thumbnail<object>>(this.GetShownModels());
                var hiddenThumbnails = _thumbnails.Where(n => !shownThumbnailSet.Contains(n)).ToArray();

                using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
                using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedTokenSource.Cancel()));

                try
                {
                    await this.ClearThumbnailAsync(hiddenThumbnails, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownThumbnailSet.Where(n => n.Image == null), width, height, false, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownThumbnailSet.Where(n => n.Image == null), width, height, true, changedTokenSource.Token);
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

    private async Task ClearThumbnailAsync(IEnumerable<Thumbnail<object>> models, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        await _applicationDispatcher.InvokeAsync(() =>
        {
            foreach (var model in models)
            {
                model.Clear();
            }
        });
    }

    private async Task LoadThumbnailAsync(IEnumerable<Thumbnail<object>> models, int width, int height, bool cacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        foreach (var thumbnail in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (thumbnail.Item is IFile file)
            {
                var options = new FileThumbnailOptions
                {
                    Width = width,
                    Height = height,
                    FormatType = ThumbnailFormatType.Png,
                    ResizeType = ThumbnailResizeType.Pad,
                    MinInterval = TimeSpan.FromSeconds(5),
                    MaxImageCount = 10
                };
                var result = await _fileThumbnailGenerator.GenerateAsync(file, options, cacheOnly, cancellationToken).ConfigureAwait(false);

                if (result.Status == FileThumbnailResultStatus.Succeeded)
                {
                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        thumbnail.Set(result.Contents);
                    });
                }
            }
            else if (thumbnail.Item is IDirectory)
            {
                var content = await _directoryThumbnailGenerator.GetThumbnailAsync(width, height, cancellationToken);
                thumbnail.Set(content);
            }
        }
    }

    private async Task RotateAsync(TimeSpan rotationSpan)
    {
        try
        {
            await Task.Delay(1).ConfigureAwait(false);

            using var canceledTokenSource = new CancellationTokenSource();
            using var canceledActionListener = _canceledActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => canceledTokenSource.Cancel()));

            for (; ; )
            {
                await Task.Delay(rotationSpan, canceledTokenSource.Token).ConfigureAwait(false);

                var models = this.GetShownModels().Where(n => n.IsRotatable).ToArray();

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    foreach (var model in models)
                    {
                        model.TryRotate();
                    }
                });
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

    private Thumbnail<object>[] GetShownModels()
    {
        var thumbnails = _thumbnails;
        var preparedThumbnailIndexSet = _preparedThumbnailIndexSet;
        if (preparedThumbnailIndexSet.Count == 0) return [];

        int minIndex = thumbnails.Length;
        int maxIndex = 0;

        foreach (var index in preparedThumbnailIndexSet)
        {
            minIndex = Math.Min(minIndex, index);
            maxIndex = Math.Max(maxIndex, index);
        }

        // padding
        minIndex = Math.Max(minIndex - 1, 0);
        maxIndex = Math.Min(maxIndex + 1, thumbnails.Length);

        var result = new List<Thumbnail<object>>();

        foreach (var thumbnail in thumbnails[minIndex..maxIndex])
        {
            result.Add(thumbnail);
        }

        return result.ToArray();
    }
}
