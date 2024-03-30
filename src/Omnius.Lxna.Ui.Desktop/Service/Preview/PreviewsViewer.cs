using System.Collections.Immutable;
using Avalonia.Threading;
using AvaloniaEdit.Utils;
using Omnius.Core;
using Omnius.Core.Avalonia;
using Omnius.Core.Collections;
using Omnius.Core.Helpers;
using Omnius.Core.Pipelines;
using Omnius.Lxna.Components.Image;
using Omnius.Lxna.Components.Storage;
using Omnius.Lxna.Components.Thumbnail;

namespace Omnius.Lxna.Ui.Desktop.Service.Preview;

public class PreviewsViewer : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly ImmutableList<IFile> _files;
    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    private Task _task = Task.CompletedTask;
    private ActionPipe _changedActionPipe = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly AsyncLock _asyncLock = new();

    public static async ValueTask<PreviewsViewer> CreateAsync(IEnumerable<IFile> files, ImageConverter imageConverter, IBytesPool bytesPool, CancellationToken cancellationToken = default)
    {
        var previewsViewer = new PreviewsViewer(files, imageConverter, bytesPool);
        await previewsViewer.InitAsync(cancellationToken);
        return previewsViewer;
    }

    private PreviewsViewer(IEnumerable<IFile> files, ImageConverter imageConverter, IBytesPool bytesPool)
    {
        _files = files.ToImmutableList();
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _changedActionPipe.Caller.Call();

        await _task;
    }

    private async ValueTask InitAsync(CancellationToken cancellationToken = default)
    {
    }

    // private async Task LoadAsync(int width, int height)
    // {
    //     try
    //     {
    //         await Task.Delay(1).ConfigureAwait(false);

    //         using var changedEvent = new AutoResetEvent(true);
    //         using var changedActionListener1 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedEvent.Set()));

    //         for (; ; )
    //         {
    //             await changedEvent.WaitAsync(canceledTokenSource.Token);

    //             using var changedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(canceledTokenSource.Token);
    //             using var changedActionListener2 = _changedActionPipe.Listener.Listen(() => ExceptionHelper.TryCatch<ObjectDisposedException>(() => changedTokenSource.Cancel()));

    //             var thumbnails = _preparedThumbnails.ToList();
    //             thumbnails.Sort((x, y) => x.Index - y.Index);

    //             try
    //             {
    //                 await this.LoadThumbnailAsync(thumbnails.Where(n => n.Image == null), width, height, true, changedTokenSource.Token);
    //                 await this.LoadThumbnailAsync(thumbnails.Where(n => n.Image == null), width, height, false, changedTokenSource.Token);
    //             }
    //             catch (OperationCanceledException)
    //             {
    //             }
    //         }
    //     }
    //     catch (OperationCanceledException)
    //     {
    //     }
    //     catch (Exception e)
    //     {
    //         _logger.Error(e);
    //     }
    // }

    public async ValueTask<Preview> GetPreviewAsync(int index, CancellationToken cancellationToken = default)
    {
        var preview = await Preview.CreateAsync(_files[index], index, _imageConverter, _bytesPool, cancellationToken);
        return preview;
    }
}
