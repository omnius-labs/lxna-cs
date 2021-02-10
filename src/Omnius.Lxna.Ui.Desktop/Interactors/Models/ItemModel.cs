using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Omnius.Core;
using Omnius.Core.Collections;
using Omnius.Core.Io;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Interactors.Models.Primitives;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Models
{
    public sealed class ItemModel : BindableBase, IDisposable
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly object _lockObject = new object();

        public ItemModel(NestedPath path)
        {
            this.Path = path;
        }

        public void Dispose()
        {
            _thumbnail?.Dispose();
            _thumbnail = null;

            foreach (var content in _thumbnailContents)
            {
                content.Dispose();
            }

            _thumbnailContents = ReadOnlyListSlim<ThumbnailContent>.Empty;
            _currentOffset = -1;
            _nextOffset = 0;

            this.RaisePropertyChanged(nameof(this.Thumbnail));
        }

        private NestedPath _path = NestedPath.Empty;

        public NestedPath Path
        {
            get => _path;
            private set
            {
                if (value == NestedPath.Empty)
                {
                    this.SetProperty(ref _path, value);
                    this.Name = string.Empty;
                    return;
                }

                this.SetProperty(ref _path, value);
                this.Name = value.GetName();
            }
        }

        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            private set => this.SetProperty(ref _name, value);
        }

        private Bitmap? _thumbnail = null;

        public Bitmap? Thumbnail
        {
            get
            {
                lock (_lockObject)
                {
                    if (_thumbnailContents.Count == 0)
                    {
                        _thumbnail?.Dispose();

                        return null;
                    }
                    else
                    {
                        if (_currentOffset == _nextOffset) return _thumbnail;

                        _thumbnail?.Dispose();

                        using var memoryStream = new RecyclableMemoryStream(BytesPool.Shared);
                        memoryStream.Write(_thumbnailContents[_nextOffset].Image.Span);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        _thumbnail = new Bitmap(memoryStream);
                        _currentOffset = _nextOffset;
                        return _thumbnail;
                    }
                }
            }
        }

        private ReadOnlyListSlim<ThumbnailContent> _thumbnailContents = ReadOnlyListSlim<ThumbnailContent>.Empty;
        private int _currentOffset = -1;
        private int _nextOffset = 0;

        public async ValueTask ClearThumbnailAsync()
        {
            if (_thumbnailContents.Count == 0) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        foreach (var content in _thumbnailContents)
                        {
                            content.Dispose();
                        }

                        _thumbnailContents = ReadOnlyListSlim<ThumbnailContent>.Empty;
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

        public async ValueTask SetThumbnailAsync(IEnumerable<ThumbnailContent> thumbnailContents)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        foreach (var content in _thumbnailContents)
                        {
                            content.Dispose();
                        }

                        _thumbnailContents = new ReadOnlyListSlim<ThumbnailContent>(thumbnailContents.ToArray());
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

        public async ValueTask RotateThumbnailAsync()
        {
            if (_thumbnailContents.Count <= 1) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        var offset = _nextOffset;
                        offset++;
                        offset %= _thumbnailContents.Count;

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
        }
    }
}
