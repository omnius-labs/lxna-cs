using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Cryptography;
using Omnius.Core.Data;
using Omnius.Core.Io;
using Omnius.Core.Network;
using Omnius.Core.Serialization;
using Omnius.Core.Serialization.Extensions;
using Omnius.Core.Serialization.RocketPack;
using Omnius.Lxna.Service.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Service
{
    public sealed class ThumbnailGenerator : AsyncDisposableBase, IThumbnailGenerator
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _configPath;
        private readonly ILiteStoreFactory _storeFactory;
        private readonly IBytesPool _bytesPool;

        private ILiteStore _liteStore;
        private readonly static HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly static HashSet<string> _videoTypeExtensionList = new HashSet<string>() { ".mp4", ".avi" };
        private readonly static Base16 _base16 = new Base16(ConvertStringCase.Lower);

        private readonly AsyncLock _asyncLock = new AsyncLock();

        internal sealed class ThumbnailGeneratorFactory : IThumbnailGeneratorFactory
        {
            public async ValueTask<IThumbnailGenerator> CreateAsync(string configPath, ILiteStoreFactory liteStoreFactory, IBytesPool bytesPool)
            {
                var result = new ThumbnailGenerator(configPath, liteStoreFactory, bytesPool);
                await result.InitAsync();

                return result;
            }
        }

        public static IThumbnailGeneratorFactory Factory { get; } = new ThumbnailGeneratorFactory();

        internal ThumbnailGenerator(string configPath, ILiteStoreFactory storeFactory, IBytesPool bytesPool)
        {
            _configPath = configPath;
            _storeFactory = storeFactory;
            _bytesPool = bytesPool;
        }

        internal async ValueTask InitAsync()
        {
            _liteStore = await _storeFactory.CreateAsync(_configPath, _bytesPool);
        }

        protected override async ValueTask OnDisposeAsync()
        {
            await _liteStore.DisposeAsync();
        }

        private static string ResizeTypeToString(ThumbnailResizeType resizeType)
        {
            return resizeType switch
            {
                ThumbnailResizeType.Crop => "crop",
                ThumbnailResizeType.Pad => "pad",
                _ => throw new NotSupportedException(nameof(resizeType)),
            };
        }

        private static string FormatTypeToString(ThumbnailFormatType formatType)
        {
            return formatType switch
            {
                ThumbnailFormatType.Png => "png",
                _ => throw new NotSupportedException(nameof(formatType)),
            };
        }

        private async ValueTask<ThumbnailGeneratorGetResult> GetPictureThumnailAsync(OmniPath omniPath, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken cancellationToken = default)
        {
            if (!OmniPath.Windows.TryDecoding(omniPath, out var path))
            {
                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
            }

            if (!_pictureTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
            }

            var fullPath = Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);

            var storePath = $"/v1/picture/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/{width}x{height}_{ResizeTypeToString(resizeType)}.{FormatTypeToString(formatType)}";
            var entry = await _liteStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken);

            if (entry != ThumbnailEntity.Empty)
            {
                if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                    && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                {
                    return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Succeeded, entry.Metadata, entry.Contents);
                }
            }

            try
            {
                using (var image = Image.Load(fullPath))
                {
                    image.Mutate(x =>
                    {
                        var resizeOptions = new ResizeOptions();
                        resizeOptions.Position = AnchorPositionMode.Center;
                        resizeOptions.Size = new SixLabors.Primitives.Size(width, height);
                        resizeOptions.Mode = resizeType switch
                        {
                            ThumbnailResizeType.Pad => ResizeMode.Pad,
                            ThumbnailResizeType.Crop => ResizeMode.Crop,
                            _ => throw new NotSupportedException(),
                        };

                        x.Resize(resizeOptions);
                    });

                    using (var stream = new RecyclableMemoryStream(_bytesPool))
                    {
                        var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
                        image.Save(stream, encoder);

                        using (var memoryOwner = stream.ToMemoryOwner())
                        {
                            var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                            var content = new ThumbnailContent(memoryOwner.Memory);
                            entry = new ThumbnailEntity(metadata, new[] { content });

                            await _liteStore.WriteAsync(storePath, entry, cancellationToken);
                        }
                    }
                }

                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Succeeded, entry.Metadata, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Info(e);
            }

            return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
        }

        private async ValueTask<ThumbnailGeneratorGetResult> GetVideoThumnailAsync(OmniPath omniPath, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken cancellationToken = default)
        {
            if (!OmniPath.Windows.TryDecoding(omniPath, out var path))
            {
                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
            }

            if (!_videoTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
            }

            // TODO
            return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
        }

        public async ValueTask<ThumbnailGeneratorGetResult> GetAsync(OmniPath omniPath, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken cancellationToken = default)
        {
            {
                var result = await this.GetPictureThumnailAsync(omniPath, width, height, formatType, resizeType, cancellationToken);

                if (result.Status == ThumbnailGeneratorGetResultStatus.Succeeded)
                {
                    return result;
                }
            }

            {
                var result = await this.GetVideoThumnailAsync(omniPath, width, height, formatType, resizeType, cancellationToken);

                if (result.Status == ThumbnailGeneratorGetResultStatus.Succeeded)
                {
                    return result;
                }
            }

            return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
        }
    }
}
