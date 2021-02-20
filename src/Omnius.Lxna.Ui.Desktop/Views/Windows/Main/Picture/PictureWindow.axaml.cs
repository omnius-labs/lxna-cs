using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Omnius.Lxna.Components.Models;
using Omnius.Lxna.Ui.Desktop.Resources;
using Omnius.Lxna.Ui.Desktop.Views.Primitives;

namespace Omnius.Lxna.Ui.Desktop.Views.Windows.Main.Picture
{
    public class PictureWindow : StatefulWindowBase
    {
        private readonly AppState? _state;
        private readonly NestedPath? _path;

        public PictureWindow(AppState state, NestedPath path)
            : this()
        {
            _state = state;
            _path = path;
        }

        public PictureWindow()
            : base()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override async ValueTask OnInitialize()
        {
            if (_state is null || _path is null) return;

            this.Model = new PictureWindowModel(_state, _path);
        }

        protected override async ValueTask OnDispose()
        {
            if (this.Model is PictureWindowModel model)
            {
                await model.DisposeAsync();
            }
        }

        public PictureWindowModel? Model
        {
            get => this.DataContext as PictureWindowModel;
            set => this.DataContext = value;
        }
    }
}
