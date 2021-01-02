using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        public ObservableCollection<DirectoryModel> Children { get; } = new ObservableCollection<DirectoryModel>();

        public async void RefreshChildren()
        {
            this.Children.Clear();

            var directories = await _fileSystem.FindDirectoriesAsync(this.Path);
            var archiveFiles = await _fileSystem.FindArchiveFilesAsync(this.Path);

            foreach (var directoryPath in CollectionHelper.Unite(directories, archiveFiles))
            {
                this.Children.Add(new DirectoryModel(directoryPath, _fileSystem));
            }
        }
    }
}
