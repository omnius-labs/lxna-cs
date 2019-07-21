using System;
using System.Collections.Generic;
using System.Text;
using Lxna.Gui.Desktop.Models;
using Omnix.Network;

namespace Lxna.Gui.Desktop.Core.Mvvm.Messages
{
    sealed class ThumbnailLoadRequest
    {
        public ThumbnailLoadRequest(FileModel fileModel, OmniAddress baseAddress)
        {
            (this.FileModel, this.BaseAddress) = (fileModel, baseAddress);
        }

        public OmniAddress BaseAddress { get; }
        public FileModel FileModel { get; }
    }
}
