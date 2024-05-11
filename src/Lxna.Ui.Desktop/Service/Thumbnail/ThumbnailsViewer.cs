using System.Collections.Immutable;
using Avalonia.Threading;
using Core.Avalonia;
using Core.Base;
using Core.Base.Helpers;
using Core.Pipelines;
using Lxna.Components.Image;
using Lxna.Components.Storage;
using SharpCompress;

namespace Lxna.Ui.Desktop.Service.Thumbnail;

public class ThumbnailsViewer : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly IDirectoryThumbnailGenerator _directoryThumbnailGenerator;
    private readonly IFileThumbnailGenerator _fileThumbnailGenerator;
    private readonly IApplicationDispatcher _applicationDispatcher;

    private ThumbnailsLoader? _thumbnailsLoader;

    private readonly AsyncLock _asyncLock = new();

    public ThumbnailsViewer(IDirectoryThumbnailGenerator directoryThumbnailGenerator, IFileThumbnailGenerator fileThumbnailGenerator, IApplicationDispatcher applicationDispatcher)
    {
        _directoryThumbnailGenerator = directoryThumbnailGenerator;
        _fileThumbnailGenerator = fileThumbnailGenerator;
        _applicationDispatcher = applicationDispatcher;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_thumbnailsLoader is not null)
        {
            await _thumbnailsLoader.DisposeAsync();
        }
    }

    public IReadOnlyList<Thumbnail> Thumbnails => _thumbnailsLoader?.Thumbnails ?? ImmutableList<Thumbnail>.Empty;
    public IReadOnlyList<Thumbnail> PreparedThumbnails => _thumbnailsLoader?.PreparedThumbnails ?? ImmutableList<Thumbnail>.Empty;

    public void SetPreparedThumbnails(IEnumerable<Thumbnail> thumbnails)
    {
        _thumbnailsLoader?.SetPreparedThumbnails(thumbnails);
    }

    public async ValueTask LoadAsync(IDirectory directory, int thumbnailWidth, int thumbnailHeight, TimeSpan rotationSpan, Comparison<object> comparison, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        using (await _asyncLock.LockAsync(cancellationToken))
        {
            var items = new List<object>();
            items.AddRange(await directory.FindFilesAsync(cancellationToken).ConfigureAwait(false));
            items.AddRange(await directory.FindDirectoriesAsync(cancellationToken).ConfigureAwait(false));
            items.Sort(comparison);

            var thumbnails = items
                .Select((item, index) => new Thumbnail(item, index, thumbnailWidth, thumbnailHeight))
                .WhereNotNull()
                .ToImmutableArray();

            if (_thumbnailsLoader is not null)
            {
                await _thumbnailsLoader.DisposeAsync().ConfigureAwait(false);
            }

            _thumbnailsLoader = await ThumbnailsLoader.CreateAsync(thumbnails, thumbnailWidth, thumbnailHeight, rotationSpan, _directoryThumbnailGenerator, _fileThumbnailGenerator, _applicationDispatcher, cancellationToken).ConfigureAwait(false);
        }
    }

    private class ThumbnailsLoader : AsyncDisposableBase
    {
        private readonly IDirectoryThumbnailGenerator _directoryThumbnailGenerator;
        private readonly IFileThumbnailGenerator _fileThumbnailGenerator;
        private readonly IApplicationDispatcher _applicationDispatcher;

        private Task _task = null!;
        private readonly ActionPipe _changedActionPipe = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public static async ValueTask<ThumbnailsLoader> CreateAsync(IEnumerable<Thumbnail> thumbnails, int width, int height, TimeSpan rotationSpan, IDirectoryThumbnailGenerator directoryThumbnailGenerator, IFileThumbnailGenerator fileThumbnailGenerator, IApplicationDispatcher applicationDispatcher, CancellationToken cancellationToken = default)
        {
            var result = new ThumbnailsLoader(thumbnails, width, height, rotationSpan, directoryThumbnailGenerator, fileThumbnailGenerator, applicationDispatcher);
            await result.InitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        private ThumbnailsLoader(IEnumerable<Thumbnail> thumbnails, int width, int height, TimeSpan rotationSpan, IDirectoryThumbnailGenerator directoryThumbnailGenerator, IFileThumbnailGenerator fileThumbnailGenerator, IApplicationDispatcher applicationDispatcher)
        {
            _directoryThumbnailGenerator = directoryThumbnailGenerator;
            _fileThumbnailGenerator = fileThumbnailGenerator;
            _applicationDispatcher = applicationDispatcher;

            this.Thumbnails = thumbnails.ToImmutableList();
            this.PreparedThumbnails = ImmutableList<Thumbnail>.Empty;
            this.Width = width;
            this.Height = height;
            this.RotationSpan = rotationSpan;
        }

        private async ValueTask InitAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            _task = Task.WhenAll(
                this.LoadAsync(_cancellationTokenSource.Token),
                this.RotateAsync(this.RotationSpan, _cancellationTokenSource.Token)
            );
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _cancellationTokenSource.Cancel();

            await _task;

            _cancellationTokenSource.Dispose();
        }

        public ImmutableList<Thumbnail> Thumbnails { get; }
        public ImmutableList<Thumbnail> PreparedThumbnails { get; private set; }
        public int Width { get; }
        public int Height { get; }
        public TimeSpan RotationSpan { get; }

        public void SetPreparedThumbnails(IEnumerable<Thumbnail> thumbnails)
        {
            var removedThumbnails = this.PreparedThumbnails.Except(thumbnails).ToArray();
            removedThumbnails.ForEach(n => n.Clear());

            this.PreparedThumbnails = thumbnails.ToImmutableList();
            _changedActionPipe.Caller.Call();
        }

        private async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1).ConfigureAwait(false);

            try
            {
                using var changedEvent = new AutoResetEvent(true);
                using var rootChangedActionListener = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedEvent.Set()));

                while (!cancellationToken.IsCancellationRequested)
                {
                    await changedEvent.WaitAsync(cancellationToken).ConfigureAwait(false);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var preparedThumbnails = this.PreparedThumbnails.ToList();
                        preparedThumbnails.Sort((x, y) => x.Index - y.Index);

                        var targetThumbnail = preparedThumbnails.FirstOrDefault(n => n.State == ThumbnailState.None);
                        if (targetThumbnail is null) break;

                        using var changedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        using var changedActionListener = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() =>
                        {
                            if (this.PreparedThumbnails.Contains(targetThumbnail)) return;
                            changedCancellationTokenSource.Cancel();
                        }));

                        try
                        {
                            await this.LoadThumbnailAsync(targetThumbnail, changedCancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                        }
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

        private async Task LoadThumbnailAsync(Thumbnail thumbnail, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (thumbnail.Item is IFile file)
            {
                var options = new FileThumbnailOptions
                {
                    Width = this.Width,
                    Height = this.Height,
                    FormatType = ImageFormatType.Png,
                    ResizeType = ImageResizeType.Min,
                    MinInterval = TimeSpan.FromSeconds(5),
                    MaxImageCount = 10
                };
                var result = await _fileThumbnailGenerator.GenerateAsync(file, options, false, cancellationToken).ConfigureAwait(false);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    if (result.Status == FileThumbnailResultStatus.Succeeded)
                    {
                        thumbnail.SetResult(result.Contents);
                    }
                    else
                    {
                        thumbnail.SetError();
                    }
                }).ConfigureAwait(false);
            }
            else if (thumbnail.Item is IDirectory dir)
            {
                var options = new DirectoryThumbnailOptions
                {
                    Width = this.Width,
                    Height = this.Height,
                    FormatType = ImageFormatType.Png,
                    ResizeType = ImageResizeType.BoxPad,
                };
                var result = await _directoryThumbnailGenerator.GenerateAsync(dir, options, cancellationToken).ConfigureAwait(false);

                await _applicationDispatcher.InvokeAsync(() =>
                {
                    if (result.Status == DirectoryThumbnailResultStatus.Succeeded && result.Content is not null)
                    {
                        thumbnail.SetResult(result.Content);
                    }
                    else
                    {
                        thumbnail.SetError();
                    }
                }).ConfigureAwait(false);
            }
        }

        private async Task RotateAsync(TimeSpan rotationSpan, CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(1).ConfigureAwait(false);

                for (; ; )
                {
                    await Task.Delay(rotationSpan, cancellationToken).ConfigureAwait(false);

                    var thumbnails = this.PreparedThumbnails.ToArray().Where(n => n.IsRotatable).ToArray();

                    await _applicationDispatcher.InvokeAsync(() =>
                    {
                        foreach (var thumbnail in thumbnails)
                        {
                            thumbnail.TryRotate();
                        }
                    }, DispatcherPriority.Background, cancellationToken).ConfigureAwait(false);
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
}
