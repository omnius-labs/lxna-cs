using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Omnius.Core;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Collections;
using Omnius.Core.Io;
using Omnius.Core.Network;
using Omnius.Lxna.Service;

namespace Omnius.Lxna.Ui.Desktop.Engine.Models
{
    public sealed class ItemModel : BindableBase, IDisposable
    {
        public ItemModel(OmniPath path)
        {
            this.Path = path;

            this.Name = this.Path.Decompose().LastOrDefault();
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
        }

        public OmniPath Path { get; }

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
                if (_thumbnailContents.Count == 0)
                {
                    _thumbnail?.Dispose();

                    return null;
                }
                else
                {
                    if (_currentOffset == _nextOffset)
                    {
                        return _thumbnail;
                    }

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

        private ReadOnlyListSlim<ThumbnailContent> _thumbnailContents = ReadOnlyListSlim<ThumbnailContent>.Empty;
        private int _currentOffset = -1;
        private int _nextOffset = 0;

        public async ValueTask ClearThumbnailAsync()
        {
            if (_thumbnailContents.Count == 0) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var content in _thumbnailContents)
                {
                    content.Dispose();
                }

                _thumbnailContents = ReadOnlyListSlim<ThumbnailContent>.Empty;
                _currentOffset = -1;
                _nextOffset = 0;

                this.OnPropertyChanged(nameof(this.Thumbnail));
            });
        }

        public async ValueTask SetThumbnailAsync(IEnumerable<ThumbnailContent> thumbnailContents)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var content in _thumbnailContents)
                {
                    content.Dispose();
                }

                _thumbnailContents = new ReadOnlyListSlim<ThumbnailContent>(thumbnailContents.ToArray());
                _currentOffset = -1;
                _nextOffset = 0;

                this.OnPropertyChanged(nameof(this.Thumbnail));
            });
        }

        public async ValueTask RotateThumbnailAsync()
        {
            if (_thumbnailContents.Count <= 1) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var offset = _nextOffset;
                offset++;
                offset %= _thumbnailContents.Count;

                if (offset != _nextOffset)
                {
                    _nextOffset = offset;
                    this.OnPropertyChanged(nameof(this.Thumbnail));
                }
            });
        }
    }
}
