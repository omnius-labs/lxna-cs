using System.Collections.Immutable;
using Avalonia.Threading;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Collections;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Service.Thumbnail;

// FIXME: use TimeProvider
public class ThumbnailsViewer : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IDirectoryThumbnailGenerator _directoryThumbnailGenerator;
    private readonly IFileThumbnailGenerator _fileThumbnailGenerator;
    private readonly IApplicationDispatcher _applicationDispatcher;

    private ImmutableArray<Thumbnail<object>> _thumbnails = ImmutableArray<Thumbnail<object>>.Empty;
    private LockedSet<Thumbnail<object>> _preparedThumbnails = new LockedSet<Thumbnail<object>>(new HashSet<Thumbnail<object>>());

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private ActionPipe _canceledActionPipe = new();

    private readonly AsyncLock _asyncLock = new();

    public ThumbnailsViewer(IDirectoryThumbnailGenerator directoryThumbnailGenerator, IFileThumbnailGenerator fileThumbnailGenerator, IApplicationDispatcher applicationDispatcher)
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
        // Index == 0はThumbnailClearingが常に呼ばれないため、除外 (Avaloniaのバグ？)
        if (thumbnail.Index == 0) return;

        lock (_preparedThumbnails.LockObject)
        {
            // ThumbnailPreparedが呼ばれた物は、必ずThumbnailClearingが呼ばれるわけではない (Avaloniaのバグ？)
            // その対策のため、著しく離れたindexが存在する場合、削除する
            foreach (var shownThumbnail in _preparedThumbnails)
            {
                if (Math.Abs(thumbnail.Index - shownThumbnail.Index) < 128) continue;

                shownThumbnail.Clear();
                _preparedThumbnails.Remove(shownThumbnail);
            }

            _preparedThumbnails.Add(thumbnail);
        }

        _changedActionPipe.Caller.Call();
    }

    public void ThumbnailClearing(Thumbnail<object> thumbnail)
    {
        thumbnail.Clear();
        _preparedThumbnails.Remove(thumbnail);

        _changedActionPipe.Caller.Call();
    }

    public async ValueTask<IReadOnlyList<Thumbnail<object>>> LoadAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan,
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

            items.Sort(comparison);

            _thumbnails = items.Select((n, i) => new Thumbnail<object>(n, i, thumbnailWidth, thumbnailHeight)).ToImmutableArray();

            var thumbnailIndexMap = ImmutableDictionary.CreateBuilder<Thumbnail<object>, int>();

            foreach (var (thumbnail, index) in _thumbnails.Select((n, i) => (n, i)))
            {
                thumbnailIndexMap.Add(thumbnail, index);
            }

            var loadTask = this.LoadAsync(thumbnailWidth, thumbnailHeight);
            var rotateTask = this.RotateAsync(rotationSpan);
            _task = Task.WhenAll(loadTask, rotateTask);

            return _thumbnails;
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

                var shownThumbnailSet = new HashSet<Thumbnail<object>>(this.ComputeShownThumbnails());

                using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
                using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedTokenSource.Cancel()));

                try
                {
                    await this.LoadThumbnailAsync(shownThumbnailSet.Where(n => n.Image == null), width, height, true, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownThumbnailSet.Where(n => n.Image == null), width, height, false, changedTokenSource.Token);
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

                var models = this.ComputeShownThumbnails().Where(n => n.IsRotatable).ToArray();

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    foreach (var model in models)
                    {
                        model.TryRotate();
                    }
                }, DispatcherPriority.Background, canceledTokenSource.Token);
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

    private Thumbnail<object>[] ComputeShownThumbnails()
    {
        var thumbnails = _thumbnails;
        var preparedThumbnails = _preparedThumbnails;
        if (preparedThumbnails.Count == 0) return [];

        int minIndex = thumbnails.Length;
        int maxIndex = 0;

        foreach (var thumbnail in preparedThumbnails)
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
