using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Collections;
using Omnius.Core.Network;
using Omnius.Lxna.Service;

namespace Lxna.Gui.Desktop.Models
{
    public sealed class ItemModel : BindableBase, IDisposable
    {
        public ItemModel(OmniPath path)
        {
            this.Path = path;

            this.Name = this.Path.Decompose().LastOrDefault();
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
            get => _thumbnail;
            private set => this.SetProperty(ref _thumbnail, value);
        }

        private ReadOnlyListSlim<Bitmap> _images { get; set; } = ReadOnlyListSlim<Bitmap>.Empty;

        public async ValueTask SetThumbnailAsync(IEnumerable<ThumbnailContent> thumbnailContents)
        {
            var oldThumbnails = this._images;

            var results = new List<Bitmap>();

            foreach (var content in thumbnailContents)
            {
                using var memoryStream = new MemoryStream();
                memoryStream.Write(content.Image.Span);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var bitmap = new Bitmap(memoryStream);

                results.Add(bitmap);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this._images = new ReadOnlyListSlim<Bitmap>(results.ToArray());
                _rotateOffset = 0;
                this.Thumbnail = results[0];
            });

            foreach (var thumbnail in oldThumbnails)
            {
                thumbnail.Dispose();
            }
        }

        private int _rotateOffset = 0;

        public async ValueTask RotateThumbnailAsync()
        {
            if (this._images.Count <= 1)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var thumbnails = this._images;

                var offset = _rotateOffset;
                offset++;
                offset %= thumbnails.Count;

                if (offset != _rotateOffset)
                {
                    this.Thumbnail = thumbnails[offset];
                    _rotateOffset = offset;
                }
            });
        }

        public void Dispose()
        {
            foreach (var thumbnail in this._images)
            {
                thumbnail.Dispose();
            }
        }
    }
}
