using System.Collections.ObjectModel;
using System.IO;

namespace Omnius.Lxna.Ui.Desktop.Interactors.Models
{
    public sealed class DirectoryModel : BindableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public DirectoryModel(string path)
        {
            this.Path = path;

            this.Name = System.IO.Path.GetFileName(this.Path);
            if (string.IsNullOrWhiteSpace(this.Name))
            {
                this.Name = path;
            }
        }

        public string Path { get; }

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

            foreach (var directoryPath in Directory.GetDirectories(this.Path, "*", SearchOption.TopDirectoryOnly))
            {
                this.Children.Add(new DirectoryModel(directoryPath));
            }
        }
    }
}
