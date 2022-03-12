using System.Collections.Immutable;
using Avalonia.Media.Imaging;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Streams;
using Omnius.Lxna.Components.ThumbnailGenerators.Models;

namespace Omnius.Lxna.Ui.Desktop.Internal.Models;

public sealed class Thumbnail<T> : BindableBase, IThumbnail<T>
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private T _target;
    private string _name = string.Empty;
    private Bitmap? _image = null;
    private ImmutableArray<ThumbnailContent> _thumbnailContents = ImmutableArray<ThumbnailContent>.Empty;
    private int _currentOffset = -1;
    private int _nextOffset = 0;

    private readonly object _lockObject = new();

    public Thumbnail(T target, string name)
    {
        _target = target;
        this.Name = name;
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

    public T Target => _target;

    public string Name
    {
        get => _name;
        private set => this.SetProperty(ref _name, value);
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

    public void Set(IEnumerable<ThumbnailContent> thumbnailContents)
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
