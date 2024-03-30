using System.Collections.Immutable;
using Omnius.Core;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
using SharpCompress;

namespace Omnius.Lxna.Ui.Desktop.Service.Preview;

public partial class PreviewsViewer : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private PreviewsLoader? _previewsLoader;

    private readonly AsyncLock _asyncLock = new();

    public PreviewsViewer(ImageConverter imageConverter, IBytesPool bytesPool)
    {
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_previewsLoader is not null)
        {
            await _previewsLoader.DisposeAsync();
        }
    }

    public IReadOnlyList<IFile> Files => _previewsLoader?.Files ?? ImmutableList<IFile>.Empty;

    public void SetSize(int width, int height)
    {
        if (_previewsLoader is null) throw new NullReferenceException();

        _previewsLoader.SetSize(width, height);
    }

    public async ValueTask<ReadOnlyMemory<byte>> GetPreviewAsync(int index, CancellationToken cancellationToken = default)
    {
        if (_previewsLoader is null) throw new NullReferenceException();

        return await _previewsLoader.GetPreviewAsync(index, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask LoadAsync(IEnumerable<IFile> files, int preloadBehindCount, int preloadAheadCount, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);

        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_previewsLoader is not null)
            {
                await _previewsLoader.DisposeAsync().ConfigureAwait(false);
            }

            _previewsLoader = await PreviewsLoader.CreateAsync(files, _imageConverter, preloadBehindCount, preloadAheadCount, _bytesPool, cancellationToken).ConfigureAwait(false);
        }
    }

    public class PreviewsLoader : AsyncDisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ImmutableList<IFile> _files;
        private readonly ImageConverter _imageConverter;
        private readonly int _preloadBehindCount;
        private readonly int _preloadAheadCount;
        private readonly IBytesPool _bytesPool;

        private int _currentIndex = -1;
        private int _currentWidth = 0;
        private int _currentHeight = 0;

        private readonly Dictionary<int, Preview> _cachedPreviewMap = new();

        private Task _task = null!;
        private readonly ActionPipe _changedActionPipe = new();
        private readonly AutoResetEvent _loadedEvent = new(false);

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly AsyncLock _asyncLock = new();

        public static async ValueTask<PreviewsLoader> CreateAsync(IEnumerable<IFile> files, ImageConverter imageConverter, int preloadBehindCount, int preloadAheadCount, IBytesPool bytesPool, CancellationToken cancellationToken = default)
        {
            var previewsLoader = new PreviewsLoader(files, imageConverter, preloadBehindCount, preloadAheadCount, bytesPool);
            await previewsLoader.InitAsync(cancellationToken).ConfigureAwait(false);
            return previewsLoader;
        }

        private PreviewsLoader(IEnumerable<IFile> files, ImageConverter imageConverter, int preloadBehindCount, int preloadAheadCount, IBytesPool bytesPool)
        {
            _files = files.ToImmutableList();
            _imageConverter = imageConverter;
            _preloadBehindCount = preloadBehindCount;
            _preloadAheadCount = preloadAheadCount;
            _bytesPool = bytesPool;
        }

        private async ValueTask InitAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            _task = this.LoadAsync(_cancellationTokenSource.Token);
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _cancellationTokenSource.Cancel();

            await _task;

            _cancellationTokenSource.Dispose();
        }

        public IReadOnlyList<IFile> Files => _files;

        public void SetSize(int width, int height)
        {
            _currentWidth = width;
            _currentHeight = height;

            _changedActionPipe.Caller.Call();
        }

        private async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await Task.Delay(1).ConfigureAwait(false);

                using var changedEvent = new AutoResetEvent(true);
                using var rootChangedActionListener = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedEvent.Set()));

                while (!cancellationToken.IsCancellationRequested)
                {
                    await changedEvent.WaitAsync(cancellationToken).ConfigureAwait(false);

                    using var changedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    using var changedActionListener = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedCancellationTokenSource.Cancel()));

                    try
                    {
                        var changedCancellationToken = changedCancellationTokenSource.Token;

                        using (await _asyncLock.LockAsync(changedCancellationToken).ConfigureAwait(false))
                        {
                            foreach (var (index, preview) in _cachedPreviewMap.ToArray())
                            {
                                if (_currentWidth == preview.Width && _currentHeight == preview.Height) continue;
                                _cachedPreviewMap.Remove(index);
                                preview.Dispose();
                            }
                        }

                        while (!changedCancellationToken.IsCancellationRequested)
                        {
                            var indexes = new List<int>();

                            foreach (var i in Enumerable.Range(_currentIndex - _preloadBehindCount, _preloadBehindCount + _preloadAheadCount + 1))
                            {
                                if (i < 0 || i >= _files.Count) continue;
                                indexes.Add(i);
                            }

                            using (await _asyncLock.LockAsync(changedCancellationToken).ConfigureAwait(false))
                            {
                                foreach (var i in _cachedPreviewMap.Keys.Where(n => !indexes.Contains(n)).ToList())
                                {
                                    var preview = _cachedPreviewMap[i];
                                    _cachedPreviewMap.Remove(i);
                                    preview.Dispose();
                                }

                                foreach (var i in indexes.ToArray())
                                {
                                    if (!_cachedPreviewMap.ContainsKey(i)) continue;
                                    indexes.Remove(i);
                                }
                            }

                            if (indexes.Count == 0) break;

                            indexes.Sort((x, y) =>
                            {
                                int c = Math.Abs(x - _currentIndex).CompareTo(Math.Abs(y - _currentIndex));
                                if (c != 0) return c;
                                return y.CompareTo(x);
                            });

                            int targetIndex = indexes.First();
                            int targetWidth = _currentWidth;
                            int targetHeight = _currentHeight;

                            {
                                var preview = await Preview.CreateAsync(_files[targetIndex], targetIndex, targetWidth, targetHeight, _imageConverter, _bytesPool, changedCancellationToken).ConfigureAwait(false);

                                using (await _asyncLock.LockAsync(changedCancellationToken).ConfigureAwait(false))
                                {
                                    _cachedPreviewMap[targetIndex] = preview;
                                }
                            }

                            _loadedEvent.Set();

                            _logger.Debug($"Loaded preview. index: {targetIndex}");
                        }
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

        public async ValueTask<ReadOnlyMemory<byte>> GetPreviewAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index < 0 || index >= _files.Count) throw new ArgumentOutOfRangeException(nameof(index));

            _currentIndex = index;
            _changedActionPipe.Caller.Call();

            for (; ; )
            {
                using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_cachedPreviewMap.TryGetValue(index, out var preview) && preview.Width == _currentWidth && preview.Height == _currentHeight)
                    {
                        return await preview.GetImageBytesAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                await _loadedEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
