using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Network;

namespace Lxna.Gui.Desktop.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        public DirectoryModel(OmniPath path)
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

        public ObservableCollection<DirectoryModel> Children { get; } = new ObservableCollection<DirectoryModel>();
    }
}
