using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media.Imaging;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Network;

namespace Lxna.Gui.Desktop.Models
{
    sealed class FileModel : BindableBase
    {
        public FileModel(OmniPath path)
        {
            this.Path = path;

            this.Name = this.Path.Decompose().LastOrDefault();
        }

        public OmniPath Path{ get; }

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
            set => this.SetProperty(ref _thumbnail, value);
        }
    }
}
