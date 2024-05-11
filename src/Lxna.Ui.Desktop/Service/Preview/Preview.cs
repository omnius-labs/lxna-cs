using System.Buffers;
using System.Runtime.ExceptionServices;
using Core.Avalonia;
using Core.Base;
using Core.Streams;
using Lxna.Components.Image;
using Lxna.Components.Storage;

namespace Lxna.Ui.Desktop.Service.Preview;

public partial class PreviewsViewer
{
    private sealed class Preview : BindableBase, IDisposable
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFile _file;
        private readonly int _index;
        private readonly int _width;
        private readonly int _height;
        private readonly ImageConverter _imageConverter;
        private readonly IBytesPool _bytesPool;

        private IMemoryOwner<byte>? _imageBytes;

        private PreviewState _previewState;
        private ExceptionDispatchInfo? _exception;

        private readonly AsyncLock _asyncLock = new();

        public static async ValueTask<Preview> CreateAsync(IFile file, int index, int width, int height, ImageConverter imageConverter, IBytesPool bytesPool, CancellationToken cancellationToken = default)
        {
            var preview = new Preview(file, index, width, height, imageConverter, bytesPool);
            await preview.InitAsync(cancellationToken).ConfigureAwait(false);
            return preview;
        }

        private Preview(IFile file, int index, int width, int height, ImageConverter imageConverter, IBytesPool bytesPool)
        {
            _file = file;
            _index = index;
            _width = width;
            _height = height;
            _imageConverter = imageConverter;
            _bytesPool = bytesPool;
        }

        private async ValueTask InitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using (var inStream = await _file.GetStreamAsync(cancellationToken).ConfigureAwait(false))
                using (var outStream = new RecyclableMemoryStream(_bytesPool))
                {
                    await _imageConverter.ConvertAsync(inStream, outStream, ImageFormatType.Png, ImageResizeType.Min, _width, _height, null, cancellationToken).ConfigureAwait(false);
                    outStream.Seek(0, SeekOrigin.Begin);

                    _imageBytes = outStream.ToMemoryOwner();
                    _previewState = PreviewState.Loaded;
                }
            }
            catch (Exception e)
            {
                _previewState = PreviewState.Error;
                _exception = ExceptionDispatchInfo.Capture(e);
            }
        }

        public void Dispose()
        {
            _imageBytes?.Dispose();
        }

        public IFile File => _file;
        public int Index => _index;
        public int Width => _width;
        public int Height => _height;
        public PreviewState State => _previewState;

        public async ValueTask<ReadOnlyMemory<byte>> GetImageBytesAsync(CancellationToken cancellationToken = default)
        {
            if (_previewState == PreviewState.Error) _exception?.Throw();

            try
            {
                return _imageBytes?.Memory ?? ReadOnlyMemory<byte>.Empty;
            }
            catch (Exception e)
            {
                _previewState = PreviewState.Error;
                _exception = ExceptionDispatchInfo.Capture(e);
                throw;
            }
        }
    }
}

public enum PreviewState
{
    None,
    Loaded,
    Error,
}
