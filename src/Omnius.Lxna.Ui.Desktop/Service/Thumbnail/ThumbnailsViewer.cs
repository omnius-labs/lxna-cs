using System.Collections.Immutable;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Collections;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Service.Thumbnail;

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
    private LockedSet<Thumbnail<object>> _shownSet = new LockedSet<Thumbnail<object>>(new HashSet<Thumbnail<object>>());

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private ActionPipe _canceledActionPipe = new();

    private readonly AsyncLock _asyncLock = new();
    private readonly object _lockObject = new();

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
        lock (_shownSet.LockObject)
        {
            // ThumbnailPreparedが呼ばれた物は、必ずThumbnailClearingが呼ばれるわけではない、らしい (Avaloniaのバグ？)
            // その対策のため、著しく離れたindexが存在する場合、削除する
            foreach (var shownThumbnail in _shownSet)
            {
                if (Math.Abs(thumbnail.Index - shownThumbnail.Index) < 128) continue;
                shownThumbnail.Clear();
                _shownSet.Remove(shownThumbnail);
            }

            _shownSet.Add(thumbnail);
        }

        _changedActionPipe.Caller.Call();
    }

    public void ThumbnailClearing(Thumbnail<object> thumbnail)
    {
        thumbnail.Clear();
        _shownSet.Remove(thumbnail);
        _changedActionPipe.Caller.Call();
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

            _thumbnails = items.Select((n, i) => new Thumbnail<object>(n, i, thumbnailWidth, thumbnailHeight)).ToImmutableArray();

            var thumbnailIndexMap = ImmutableDictionary.CreateBuilder<Thumbnail<object>, int>();

            foreach (var (thumbnail, index) in _thumbnails.Select((n, i) => (n, i)))
            {
                thumbnailIndexMap.Add(thumbnail, index);
            }

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

                using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
                using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedTokenSource.Cancel()));

                try
                {
                    await this.LoadThumbnailAsync(shownThumbnailSet.Where(n => n.Image == null), width, height, false, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownThumbnailSet.Where(n => n.Image == null), width, height, true, changedTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
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
                    FormatType = ImageFormatType.Png,
                    ResizeType = ImageResizeType.Min,
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
            else if (thumbnail.Item is IDirectory dir)
            {
                var options = new DirectoryThumbnailOptions
                {
                    Width = width,
                    Height = height,
                    FormatType = ImageFormatType.Png,
                    ResizeType = ImageResizeType.Min,
                };
                var result = await _directoryThumbnailGenerator.GenerateAsync(dir, options, cancellationToken);

                if (result.Status == DirectoryThumbnailResultStatus.Succeeded && result.Content is not null)
                {
                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        thumbnail.Set(result.Content);
                    });
                }
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
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }

    private Thumbnail<object>[] GetShownModels()
    {
        var thumbnails = _thumbnails;
        var shownSet = _shownSet;
        if (shownSet.Count == 0) return [];

        int minIndex = thumbnails.Length;
        int maxIndex = 0;

        foreach (var thumbnail in shownSet)
        {
            minIndex = Math.Min(minIndex, thumbnail.Index);
            maxIndex = Math.Max(maxIndex, thumbnail.Index);
        }

        // 稀に前後1要素が欠けていることがある、Avaloniaのバグだと思われるため対応
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
