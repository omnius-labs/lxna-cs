using System.Collections.Immutable;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.IconGenerators;
using Omnius.Lxna.Components.IconGenerators.Models;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;
using Omnius.Lxna.Ui.Desktop.Internal.Models;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Internal;

public record struct ThumbnailsViewerLoadResult
{
    public ImmutableArray<Thumbnail> Thumbnails { get; init; }
}

public class ThumbnailsViewer : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly FileThumbnailGenerator _fileThumbnailGenerator;
    private readonly IApplicationDispatcher _applicationDispatcher;

    private ImmutableArray<Thumbnail> _thumbnails = ImmutableArray<Thumbnail>.Empty;
    private ImmutableDictionary<Thumbnail, int> _thumbnailIndexMap = ImmutableDictionary<Thumbnail, int>.Empty;
    private ImmutableHashSet<int> _preparedThumbnailIndexSet = ImmutableHashSet<int>.Empty;

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private ActionPipe _canceledActionPipe = new();

    private readonly AsyncLock _asyncLock = new();

    public ThumbnailsViewer(FileThumbnailGenerator fileThumbnailGenerator, IApplicationDispatcher applicationDispatcher)
    {
        _fileThumbnailGenerator = fileThumbnailGenerator;
        _applicationDispatcher = applicationDispatcher;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await _task;
    }

    public void ThumbnailPrepared(Thumbnail thumbnail)
    {
        if (_thumbnailIndexMap.TryGetValue(thumbnail, out var index))
        {
            _preparedThumbnailIndexSet = _preparedThumbnailIndexSet.Add(index);
            _changedActionPipe.Caller.Call();
        }
    }

    public void ThumbnailClearing(Thumbnail thumbnail)
    {
        if (_thumbnailIndexMap.TryGetValue(thumbnail, out var index))
        {
            _preparedThumbnailIndexSet = _preparedThumbnailIndexSet.Remove(index);
            _changedActionPipe.Caller.Call();
        }
    }

    public async ValueTask<ThumbnailsViewerLoadResult> LoadAsync(IDirectory directory,
        int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan,
        Comparison<IFile> comparison, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            _canceledActionPipe.Caller.Call();

            await _task;

            var thumbnails = new List<Thumbnail>();

            foreach (var file in await directory.FindFilesAsync(cancellationToken))
            {
                thumbnails.Add(new Thumbnail(file));
            }

            thumbnails.Sort((x, y) => comparison.Invoke(x.File, y.File));
            _thumbnails = thumbnails.ToImmutableArray();

            var thumbnailIndexMap = ImmutableDictionary.CreateBuilder<Thumbnail, int>();

            foreach (var (file, index) in thumbnails.Select((n, i) => (n, i)))
            {
                thumbnailIndexMap.Add(file, index);
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

                var shownThumbnailSet = new HashSet<Thumbnail>(this.GetShownModels());
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

    private async Task ClearThumbnailAsync(IEnumerable<Thumbnail> models, CancellationToken cancellationToken = default)
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

    private async Task LoadThumbnailAsync(IEnumerable<Thumbnail> models, int width, int height, bool cacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        foreach (var thumbnail in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new FileThumbnailOptions
            {
                Width = width,
                Height = height,
                FormatType = ThumbnailFormatType.Png,
                ResizeType = ThumbnailResizeType.Pad,
                MinInterval = TimeSpan.FromSeconds(5),
                MaxImageCount = 10
            };
            var result = await _fileThumbnailGenerator.GenerateAsync(thumbnail.File, options, cacheOnly, cancellationToken).ConfigureAwait(false);

            if (result.Status == FileThumbnailResultStatus.Succeeded)
            {
                await _applicationDispatcher.InvokeAsync(() =>
                {
                    thumbnail.Set(result.Contents);
                });
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

    private Thumbnail[] GetShownModels()
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

        var result = new List<Thumbnail>();

        foreach (var thumbnail in thumbnails[minIndex..maxIndex])
        {
            result.Add(thumbnail);
        }

        return result.ToArray();
    }
}
