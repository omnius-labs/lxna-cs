using System.Collections.Immutable;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Service.Thumbnail;

public sealed class Thumbnail<T> : BindableBase
    where T : notnull
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private T _item;
    private readonly int _index;
    private double _width;
    private double _height;
    private Bitmap? _image = null;
    private ImmutableArray<ThumbnailContent> _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
    private int _currentOffset = -1;
    private int _nextOffset = 0;

    private readonly object _lockObject = new();

    public Thumbnail(T item, int index, double width, double height)
    {
        _item = item;
        _index = index;
        _width = width;
        _height = height;
    }

    public void Dispose()
    {
        _image?.Dispose();
        _image = null;

        foreach (var content in _thumbnailContents)
        {
            content.Image.Dispose();
        }

        _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
        _currentOffset = -1;
        _nextOffset = 0;

        this.RaisePropertyChanged(nameof(this.Image));
    }

    public T Item => _item;
    public int Index => _index;
    public double Width => _width;
    public double Height => _height;

    public string Name
    {
        get
        {
            if (_item is IFile file)
            {
                return file.Name;
            }
            else if (_item is IDirectory directory)
            {
                return directory.Name;
            }

            throw new NotSupportedException($"not support {_item!.GetType()}");
        }
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

    public void Set(ThumbnailContent content)
    {
        this.Set([content]);
    }

    public void Set(IEnumerable<ThumbnailContent> contents)
    {
        lock (_lockObject)
        {
            foreach (var content in _thumbnailContents)
            {
                content.Image.Dispose();
            }

            _thumbnailContents = contents.ToImmutableArray();
            _currentOffset = -1;
            _nextOffset = 0;

            this.RaisePropertyChanged(nameof(this.Image));
        }
    }

    public void Clear()
    {
        this.Set(Enumerable.Empty<ThumbnailContent>());
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
