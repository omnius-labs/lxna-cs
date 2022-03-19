using System.Collections.Immutable;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;
using Omnius.Lxna.Ui.Desktop.Internal.Models;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Internal;

public record struct ThumbnailsViewerStartResult
{
    public ThumbnailsViewerStartResult(ImmutableArray<IThumbnail<IFile>> fileThumbnails, ImmutableArray<IThumbnail<IDirectory>> directoryThumbnails)
    {
        this.FileThumbnails = fileThumbnails;
        this.DirectoryThumbnails = directoryThumbnails;
    }

    public ImmutableArray<IThumbnail<IFile>> FileThumbnails { get; }

    public ImmutableArray<IThumbnail<IDirectory>> DirectoryThumbnails { get; }
}

public interface IThumbnailsViewer : IAsyncDisposable
{
    void ItemPrepared(IThumbnail<object> thumbnail);

    void ItemClearing(IThumbnail<object> thumbnail);

    ValueTask<ThumbnailsViewerStartResult> StartAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan, CancellationToken cancellationToken = default);
}

public class ThumbnailsViewer : AsyncDisposableBase, IThumbnailsViewer
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IApplicationDispatcher _applicationDispatcher;

    private ImmutableArray<IThumbnail<object>> _models = ImmutableArray<IThumbnail<object>>.Empty;
    private ImmutableHashSet<IThumbnail<object>> _shownModelSet = ImmutableHashSet<IThumbnail<object>>.Empty;

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private ActionPipe _canceledActionPipe = new();

    private readonly AsyncLock _asyncLock = new();

    public ThumbnailsViewer(IThumbnailGenerator thumbnailGenerator, IApplicationDispatcher applicationDispatcher)
    {
        _thumbnailGenerator = thumbnailGenerator;
        _applicationDispatcher = applicationDispatcher;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await _task;
    }

    public void ItemPrepared(IThumbnail<object> thumbnail)
    {
        _shownModelSet = _shownModelSet.Add(thumbnail);

        _changedActionPipe.Caller.Call();
    }

    public void ItemClearing(IThumbnail<object> thumbnail)
    {
        _shownModelSet = _shownModelSet.Remove(thumbnail);

        _changedActionPipe.Caller.Call();
    }

    public async ValueTask<ThumbnailsViewerStartResult> StartAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            _canceledActionPipe.Caller.Call();

            await _task;

            var files = new List<IThumbnail<IFile>>();
            var dirs = new List<IThumbnail<IDirectory>>();

            foreach (var file in await directory.FindFilesAsync(cancellationToken))
            {
                files.Add(new Thumbnail<IFile>(file, file.Name));
            }

            foreach (var dir in await directory.FindDirectoriesAsync(cancellationToken))
            {
                dirs.Add(new Thumbnail<IDirectory>(dir, dir.Name));
            }

            files.Sort((x, y) => x.Name.CompareTo(y.Name));
            dirs.Sort((x, y) => x.Name.CompareTo(y.Name));

            _models = CollectionHelper.Unite<IThumbnail<object>>(files, dirs).ToImmutableArray();

            var loadTask = this.LoadAsync(thumbnailWidth, thumbnailHeight);
            var rotateTask = this.RotateAsync(rotationSpan);
            _task = Task.WhenAll(loadTask, rotateTask);

            return new ThumbnailsViewerStartResult(files.ToImmutableArray(), dirs.ToImmutableArray());
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

                var shownModelSet = new HashSet<IThumbnail<object>>(this.GetShownModels());
                var hiddenModels = _models.Where(n => !shownModelSet.Contains(n)).ToArray();

                using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
                using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedTokenSource.Cancel()));

                try
                {
                    await this.ClearThumbnailAsync(hiddenModels, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownModelSet.Where(n => n.Image == null), width, height, false, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(shownModelSet.Where(n => n.Image == null), width, height, true, changedTokenSource.Token);
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

    private async Task ClearThumbnailAsync(IEnumerable<IThumbnail<object>> models, CancellationToken cancellationToken = default)
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

    private async Task LoadThumbnailAsync(IEnumerable<IThumbnail<object>> models, int width, int height, bool cacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        foreach (var model in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new ThumbnailGeneratorGetThumbnailOptions(width, height, ThumbnailFormatType.Png, ThumbnailResizeType.Pad, TimeSpan.FromSeconds(5), 5);

            if (model is Thumbnail<IFile> fileThumbnail)
            {
                var result = await _thumbnailGenerator.GetThumbnailAsync(fileThumbnail.Target, options, cacheOnly, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorGetThumbnailResultStatus.Succeeded)
                {
                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        model.Set(result.Contents);
                    });
                }
            }
            else if (model is Thumbnail<IDirectory> dirThumbnail)
            {
                var result = await _thumbnailGenerator.GetThumbnailAsync(dirThumbnail.Target, options, cacheOnly, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorGetThumbnailResultStatus.Succeeded)
                {
                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        model.Set(result.Contents);
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
        catch (OperationCanceledException e)
        {
            _logger.Debug(e);
        }
        catch (Exception e)
        {
            _logger.Error(e);
        }
    }

    private IThumbnail<object>[] GetShownModels()
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

        var result = new List<IThumbnail<object>>();

        foreach (var (model, index) in models.Select((n, i) => (n, i)))
        {
            if (index < minIndex || index > maxIndex) continue;

            result.Add(model);
        }

        return result.OrderBy(n => !shownModelSet.Contains(n)).ToArray();
    }
}
