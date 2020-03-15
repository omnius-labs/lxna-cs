using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Omnius.Core.Avalonia.Models.Primitives;
using Omnius.Core.Network;

namespace Omnius.Lxna.Ui.Desktop.Engine.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

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

        public void RefreshChildren()
        {
            this.Children.Clear();

            foreach (var directoryPath in Directory.GetDirectories(this.Path.ToCurrentPlatformPath(), "*", SearchOption.TopDirectoryOnly))
            {
                this.Children.Add(new DirectoryModel(OmniPath.FromCurrentPlatformPath(directoryPath)));
            }
        }
    }
}
