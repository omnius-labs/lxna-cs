using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media.Imaging;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Collections;
using Omnius.Core.Network;
using Omnius.Lxna.Service;

namespace Lxna.Gui.Desktop.Models
{
    sealed class FileModel : BindableBase
    {
        public FileModel(OmniPath path)
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
        
        public ReadOnlyListSlim<ThumbnailContent> ThumbnailContents { get; private set; }

        public void SetThumbnailContents(IEnumerable<ThumbnailContent> thumbnailContents)
        {
            this.ThumbnailContents = new ReadOnlyListSlim<ThumbnailContent>(thumbnailContents.ToArray());
        }

        private int _rotateOffset = 0;

        public void RotateThumbnail()
        {
            var tmp = _rotateOffset;
            tmp++;
            tmp %= this.ThumbnailContents.Count;

            if(this.Thumbnail != null && tmp == _rotateOffset)
            {
                return;
            }
            _rotateOffset = tmp;

            this.Thumbnail?.Dispose();

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(this.ThumbnailContents[_rotateOffset].Image.Span);
                memoryStream.Seek(0, SeekOrigin.Begin);

                var bitmap = new Bitmap(memoryStream);
                this.Thumbnail = bitmap;
            }
        }
    }
}
