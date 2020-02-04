using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Omnix.Avalonia.Controls;
using Omnix.Avalonia.Controls.Presenters;

namespace Lxna.Gui.Desktop.Windows.Main
{
    public class MainWindow : Window
    {
        private bool _initialized = false;

        public MainWindow()
        {
            this.DataContext = new MainWindowViewModel();

            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            var listBox = this.FindControl<ListBox>("ListBox");
            listBox.PropertyChanged += this.ListBox_PropertyChanged;
        }

        private void ListBox_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_initialized)
            {
                return;
            }

            var listBox = this.FindControl<ListBox>("ListBox");
            var panel = listBox.GetVisualDescendants().OfType<VirtualizingWrapPanel>().FirstOrDefault();

            if (panel != null)
            {
                ((CustomItemVirtualizer)panel.Controller).ChildrenChanged += (sender2, e2) =>
                {
                    var children = panel.Children.ToList();
                    Debug.WriteLine($"{DateTime.Now.ToString()}: {children.Count}");
                };

                _initialized = true;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void HandleClosed()
        {
            base.HandleClosed();

            if (this.DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
