using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public DirectoryModel(string path)
        {
            this.Path = path;
        }

        private string _path = string.Empty;

        public string Path
        {
            get => _path;
            private set
            {
                if (value == string.Empty)
                {
                    this.SetProperty(ref _path, value);
                    this.Name = value;
                    return;
                }

                var fullPath = System.IO.Path.GetFullPath(value);
                this.SetProperty(ref _path, fullPath);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (fullPath == System.IO.Path.GetPathRoot(fullPath))
                    {
                        this.Name = fullPath;
                    }
                    else
                    {
                        this.Name = System.IO.Path.GetFileName(this.Path);
                    }
                }
            }
        }

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

            var list = Directory.GetDirectories(this.Path, "*", SearchOption.TopDirectoryOnly).ToList();
            list.Sort();

            foreach (var directoryPath in list)
            {
                this.Children.Add(new DirectoryModel(directoryPath));
            }
        }

        public bool IsContainsSubDirectories()
        {
            try
            {
                return Directory.EnumerateDirectories(this.Path).Any();
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }

            return false;
        }
    }
}
