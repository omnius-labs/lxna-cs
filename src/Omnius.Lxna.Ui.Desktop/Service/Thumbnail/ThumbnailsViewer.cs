using System.Collections.Immutable;
using Avalonia.Threading;
using AvaloniaEdit.Utils;
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

    private ImmutableArray<Thumbnail> _thumbnails = ImmutableArray<Thumbnail>.Empty;
    private LockedSet<Thumbnail> _preparedThumbnails = new LockedSet<Thumbnail>(new HashSet<Thumbnail>());

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
        _canceledActionPipe.Caller.Call();

        await _task;
    }

    public IReadOnlyList<Thumbnail> Thumbnails => _thumbnails;

    public void SetPreparedThumbnails(IEnumerable<Thumbnail> thumbnails)
    {
        lock (_preparedThumbnails.LockObject)
        {
            var removedThumbnails = _preparedThumbnails.Except(thumbnails).ToArray();

            foreach (var thumbnail in removedThumbnails)
            {
                thumbnail.Clear();
                _preparedThumbnails.Remove(thumbnail);
            }

            var addedThumbnails = thumbnails.Except(_preparedThumbnails).ToArray();
            _preparedThumbnails.AddRange(addedThumbnails);
        }

        _changedActionPipe.Caller.Call();
    }

    public async ValueTask LoadAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan, Comparison<object> comparison, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            _canceledActionPipe.Caller.Call();

            await _task;

            var items = new List<object>();
            items.AddRange(await directory.FindFilesAsync(cancellationToken));
            items.AddRange(await directory.FindDirectoriesAsync(cancellationToken));
            items.Sort(comparison);

            _thumbnails = items
                .Select((n, i) =>
                {
                    return n switch
                    {
                        IFile file => new Thumbnail(file, file.Name, i, thumbnailWidth, thumbnailHeight),
                        IDirectory dir => new Thumbnail(dir, dir.Name, i, thumbnailWidth, thumbnailHeight),
                        _ => null,
                    };
                })
                .WhereNotNull()
                .ToImmutableArray();

            var loadTask = this.LoadAsync(thumbnailWidth, thumbnailHeight);
            var rotateTask = this.RotateAsync(rotationSpan);
            _task = Task.WhenAll(loadTask, rotateTask);
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

                using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
                using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedTokenSource.Cancel()));

                var thumbnails = _preparedThumbnails.ToList();
                thumbnails.Sort((x, y) => x.Index - y.Index);

                try
                {
                    await this.LoadThumbnailAsync(thumbnails.Where(n => n.Image == null), width, height, true, changedTokenSource.Token);
                    await this.LoadThumbnailAsync(thumbnails.Where(n => n.Image == null), width, height, false, changedTokenSource.Token);
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

    private async Task LoadThumbnailAsync(IEnumerable<Thumbnail> models, int width, int height, bool cacheOnly, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);

        foreach (var thumbnail in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (thumbnail.Tag is IFile file)
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
            else if (thumbnail.Tag is IDirectory dir)
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

                var thumbnails = _preparedThumbnails.ToArray().Where(n => n.IsRotatable).ToArray();

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    foreach (var thumbnail in thumbnails)
                    {
                        thumbnail.TryRotate();
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
}
