using System.Collections.Immutable;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail.Models;

namespace Omnius.Lxna.Ui.Desktop.Internal.Models;

public sealed class FileThumbnail : BindableBase, IThumbnail
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private string _name = string.Empty;
    private Bitmap? _thumbnail = null;
    private ImmutableArray<ThumbnailContent> _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
    private int _currentOffset = -1;
    private int _nextOffset = 0;

    private readonly object _lockObject = new();

    public FileThumbnail(IFile file)
    {
        this.File = file;
    }

    public void Dispose()
    {
        _thumbnail?.Dispose();
        _thumbnail = null;

        foreach (var content in _thumbnailContents)
        {
            content.Image.Dispose();
        }

        _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
        _currentOffset = -1;
        _nextOffset = 0;

        this.RaisePropertyChanged(nameof(this.Thumbnail));
    }

    public IFile File { get; }

    public string Name
    {
        get => _name;
        private set => this.SetProperty(ref _name, value);
    }

    public Bitmap? Thumbnail
    {
        get
        {
            lock (_lockObject)
            {
                if (_thumbnailContents.Length == 0)
                {
                    _thumbnail?.Dispose();
                    _thumbnail = null;

                    return null;
                }
                else
                {
                    if (_currentOffset == _nextOffset) return _thumbnail;

                    _thumbnail?.Dispose();

                    using var memoryStream = new RecyclableMemoryStream(BytesPool.Shared);
                    memoryStream.Write(_thumbnailContents[_nextOffset].Image.Memory.Span);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    _thumbnail = new Bitmap(memoryStream);
                    _currentOffset = _nextOffset;
                    return _thumbnail;
                }
            }
        }
    }

    public bool IsRotatableThumbnail => _thumbnailContents.Length > 1;

    public async ValueTask SetThumbnailAsync(IEnumerable<ThumbnailContent> thumbnailContents, CancellationToken cancellationToken = default)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                lock (_lockObject)
                {
                    foreach (var content in _thumbnailContents)
                    {
                        content.Image.Dispose();
                    }

                    _thumbnailContents = thumbnailContents.ToImmutableArray();
                    _currentOffset = -1;
                    _nextOffset = 0;

                    this.RaisePropertyChanged(nameof(this.Thumbnail));
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        });
    }

    public async ValueTask ClearThumbnailAsync(CancellationToken cancellationToken = default)
    {
        await this.SetThumbnailAsync(Enumerable.Empty<ThumbnailContent>(), cancellationToken);
    }

    public async ValueTask<bool> TryRotateThumbnailAsync(CancellationToken cancellationToken = default)
    {
        if (_thumbnailContents.Length <= 1) return false;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                lock (_lockObject)
                {
                    var offset = _nextOffset;
                    offset++;
                    offset %= _thumbnailContents.Length;

                    if (offset != _nextOffset)
                    {
                        _nextOffset = offset;
                        this.RaisePropertyChanged(nameof(this.Thumbnail));
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        });

        return true;
    }
}
