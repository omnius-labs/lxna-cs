using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Omnius.Core.Helpers;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IFileSystem _fileSystem;

        public DirectoryModel(NestedPath path, IFileSystem fileSystem)
        {
            this.Path = path;
            _fileSystem = fileSystem;

            if (path == NestedPath.Empty)
            {
                return;
            }

            this.Children = new[] { new DirectoryModel(NestedPath.Empty, _fileSystem) };
        }

        private NestedPath _path = NestedPath.Empty;

        public NestedPath Path
        {
            get => _path;
            private set
            {
                if (value == NestedPath.Empty)
                {
                    this.SetProperty(ref _path, value);
                    this.Name = string.Empty;
                    return;
                }

                this.SetProperty(ref _path, value);
                this.Name = value.GetName();
            }
        }

        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            private set => this.SetProperty(ref _name, value);
        }

        private bool _isExpanded = false;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                this.SetProperty(ref _isExpanded, value);
                this.RefreshChildrenAsync();
            }
        }

        private IList<DirectoryModel> _children = Array.Empty<DirectoryModel>();

        public IList<DirectoryModel> Children
        {
            get => _children;
            set => this.SetProperty(ref _children, value);
        }

        public async void RefreshChildrenAsync()
        {
            var directories = await _fileSystem.FindDirectoriesAndArchiveFilesAsync(this.Path);

            var children = new List<DirectoryModel>();

            foreach (var directoryPath in directories)
            {
                children.Add(new DirectoryModel(directoryPath, _fileSystem));
            }

            this.Children = children;
        }
    }
}
