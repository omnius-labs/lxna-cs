using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Omnius.Lxna.Components;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Interactors.Models.Primitives;

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

            if (path == NestedPath.Empty) return;

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

        // FIXME
        public async void RefreshChildrenAsync()
        {
            var children = new List<DirectoryModel>();

            foreach (var directoryPath in await _fileSystem.FindDirectoriesAsync(this.Path))
            {
                children.Add(new DirectoryModel(directoryPath, _fileSystem));
            }

            foreach (var archiveFilePath in await _fileSystem.FindArchiveFilesAsync(this.Path))
            {
                children.Add(new DirectoryModel(archiveFilePath, _fileSystem));
            }

            this.Children = children;
        }
    }
}
