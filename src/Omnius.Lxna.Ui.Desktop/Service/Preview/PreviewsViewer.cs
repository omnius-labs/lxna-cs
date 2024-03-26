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

public class PreviewsViewer
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly ImmutableList<IFile> _files;
    private readonly ImageConverter _imageConverter;
    private readonly IBytesPool _bytesPool;

    public PreviewsViewer(IEnumerable<IFile> files, ImageConverter imageConverter, IBytesPool bytesPool)
    {
        _files = files.ToImmutableList();
        _imageConverter = imageConverter;
        _bytesPool = bytesPool;
    }

    public async ValueTask<Preview> GetPreviewAsync(int index, CancellationToken cancellationToken = default)
    {
        var preview = new Preview(file, _imageConverter, _bytesPool);
        await preview.InitAsync(cancellationToken);
        return preview;
    }
}
