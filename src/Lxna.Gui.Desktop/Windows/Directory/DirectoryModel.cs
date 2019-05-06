using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Lxna.Messages;
using Omnix.Avalonia;
using Omnix.Avalonia.ViewModels;

namespace Lxna.Gui.Desktop.Windows
{
    sealed class DirectoryModel : BindableBase
    {
        public DirectoryModel(ContentId contentId)
        {
            this.ContentId = contentId;

            if (this.ContentId.Path.Length == 3)
            {
                this.Name = this.ContentId.Path;
            }
            else
            {
                this.Name = Path.GetFileName(this.ContentId.Path);
            }
        }

        public ContentId ContentId { get; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => this.SetProperty(ref _name, value);
        }

        public ObservableCollection<DirectoryModel> Children { get; } = new ObservableCollection<DirectoryModel>();
    }
}
