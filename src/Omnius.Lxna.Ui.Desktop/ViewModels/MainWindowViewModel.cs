using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Lxna.Components;

namespace Omnius.Lxna.Ui.Desktop.ViewModels
{
    public class MainWindowViewModel : AsyncDisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IThumbnailGenerator _thumbnailGenerator;

        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public MainWindowViewModel(IThumbnailGenerator thumbnailGenerator)
        {
            _thumbnailGenerator = thumbnailGenerator;
            this.SearchControlViewModel = new SearchControlViewModel(_thumbnailGenerator);
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _disposable.Dispose();
            await this.SearchControlViewModel.DisposeAsync();
        }

        public SearchControlViewModel SearchControlViewModel { get; }
    }
}
