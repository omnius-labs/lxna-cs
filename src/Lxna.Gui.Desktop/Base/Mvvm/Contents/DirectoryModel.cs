using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Lxna.Gui.Desktop.Base.Mvvm.Primitives;
using Lxna.Messages;

namespace Lxna.Gui.Desktop.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        public DirectoryModel(LxnaContentClue contentClue)
        {
            this.ContentClue = contentClue;

            this.Name = this.ContentClue.Name;
        }

        public LxnaContentClue ContentClue { get; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => this.SetProperty(ref _name, value);
        }

        public ObservableCollection<DirectoryModel> Children { get; } = new ObservableCollection<DirectoryModel>();
    }
}
