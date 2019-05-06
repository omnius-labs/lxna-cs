using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Lxna.Messages;
using Omnix.Avalonia;
using Omnix.Avalonia.ViewModels;

namespace Lxna.Gui.Desktop.Windows
{
    sealed class FileModel : BindableBase
    {
        public FileModel(ContentId contentId)
        {
            this.ContentId = contentId;

            this.Name = Path.GetFileName(this.ContentId.Path);
        }

        public ContentId ContentId { get; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => this.SetProperty(ref _name, value);
        }

        private Bitmap? _thumbnailBitmap = null;
        public Bitmap? Thumbnail
        {
            get
            {
                Debug.WriteLine(this.ContentId.Path);

                return _thumbnailBitmap;
            }
            set => this.SetProperty(ref _thumbnailBitmap, value);
        }
    }
}
