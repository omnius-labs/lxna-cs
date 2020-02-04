using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Lxna.Messages;
using Omnix.Avalonia.Models.Primitives;

namespace Lxna.Gui.Desktop.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        public DirectoryModel(LxnaContentId contentId)
        {
            this.ContentId = contentId;

            this.Name = this.ContentId.Name;
        }

        public LxnaContentId ContentId { get; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => this.SetProperty(ref _name, value);
        }

        public ObservableCollection<DirectoryModel> Children { get; } = new ObservableCollection<DirectoryModel>();
    }
}
