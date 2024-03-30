using System.Collections.Immutable;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Service.Thumbnail;

public sealed class Thumbnail : BindableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly object _item;
    private readonly int _index;
    private readonly double _width;
    private readonly double _height;

    private bool _isSelected = false;
    private ThumbnailState _state = ThumbnailState.None;
    private Bitmap? _image = null;
    private ImmutableArray<ThumbnailContent> _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
    private int _currentOffset = -1;
    private int _nextOffset = 0;

    private readonly object _lockObject = new();

    public Thumbnail(object tag, int index, double width, double height)
    {
        _item = tag;
        _index = index;
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        this.Cleanup();
    }

    private void Cleanup()
    {
        _image?.Dispose();
        _image = null;

        if (_thumbnailContents.Length > 0)
        {
            _thumbnailContents.Dispose();
            _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
        }

        _currentOffset = -1;
        _nextOffset = 0;
    }

    public object Item => _item;

    public string Name
    {
        get
        {
            return _item switch
            {
                IFile file => file.Name,
                IDirectory directory => directory.Name,
                _ => string.Empty,
            };
        }
    }

    public int Index => _index;
    public double Width => _width;
    public double Height => _height;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.SetProperty(ref _isSelected, value);
    }

    public ThumbnailState State
    {
        get => _state;
        private set => this.SetProperty(ref _state, value);
    }

    public Bitmap? Image
    {
        get
        {
            lock (_lockObject)
            {
                if (_thumbnailContents.Length == 0)
                {
                    _image?.Dispose();
                    _image = null;

                    return null;
                }
                else
                {
                    if (_currentOffset == _nextOffset) return _image;

                    _image?.Dispose();

                    using var memoryStream = new RecyclableMemoryStream(BytesPool.Shared);
                    memoryStream.Write(_thumbnailContents[_nextOffset].Image.Memory.Span);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    _image = new Bitmap(memoryStream);
                    _currentOffset = _nextOffset;
                    return _image;
                }
            }
        }
    }

    public bool IsRotatable => _thumbnailContents.Length > 1;

    public void SetResult(ThumbnailContent content)
    {
        this.SetResult([content]);
    }

    public void SetResult(IEnumerable<ThumbnailContent> contents)
    {
        lock (_lockObject)
        {
            _thumbnailContents.Dispose();
            _thumbnailContents = contents.ToImmutableArray();

            _currentOffset = -1;
            _nextOffset = 0;

            this.RaisePropertyChanged(nameof(this.Image));
            this.State = ThumbnailState.Loaded;
        }
    }

    public void SetError()
    {
        lock (_lockObject)
        {
            this.Cleanup();

            this.RaisePropertyChanged(nameof(this.Image));
            this.State = ThumbnailState.Error;
        }
    }

    public void Clear()
    {
        lock (_lockObject)
        {
            this.Cleanup();

            this.RaisePropertyChanged(nameof(this.Image));
            this.State = ThumbnailState.None;
        }
    }

    public bool TryRotate()
    {
        if (_thumbnailContents.Length <= 1) return false;

        lock (_lockObject)
        {
            var offset = _nextOffset;
            offset++;
            offset %= _thumbnailContents.Length;

            if (offset != _nextOffset)
            {
                _nextOffset = offset;
                this.RaisePropertyChanged(nameof(this.Image));
            }
        }

        return true;
    }
}

public enum ThumbnailState
{
    None,
    Loaded,
    Error,
}
