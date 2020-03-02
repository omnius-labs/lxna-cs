using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Cryptography;
using Omnius.Core.Data;
using Omnius.Core.Extensions;
using Omnius.Core.Io;
using Omnius.Core.Network;
using Omnius.Core.Serialization;
using Omnius.Core.Serialization.Extensions;
using Omnius.Core.Serialization.RocketPack;
using Omnius.Lxna.Service.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Service
{
    public sealed class ThumbnailGenerator : AsyncDisposableBase, IThumbnailGenerator
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _configPath;
        private readonly IObjectStoreFactory _storeFactory;
        private readonly IBytesPool _bytesPool;

        private IObjectStore _objectStore;
        private readonly static HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly static HashSet<string> _movieTypeExtensionList = new HashSet<string>() { ".mp4", ".avi" };
        private readonly static Base16 _base16 = new Base16(ConvertStringCase.Lower);

        private readonly AsyncLock _asyncLock = new AsyncLock();

        internal sealed class ThumbnailGeneratorFactory : IThumbnailGeneratorFactory
        {
            public async ValueTask<IThumbnailGenerator> CreateAsync(string configPath, IObjectStoreFactory objectStoreFactory, IBytesPool bytesPool)
            {
                var result = new ThumbnailGenerator(configPath, objectStoreFactory, bytesPool);
                await result.InitAsync();

                return result;
            }
        }

        public static IThumbnailGeneratorFactory Factory { get; } = new ThumbnailGeneratorFactory();

        internal ThumbnailGenerator(string configPath, IObjectStoreFactory storeFactory, IBytesPool bytesPool)
        {
            _configPath = configPath;
            _storeFactory = storeFactory;
            _bytesPool = bytesPool;
        }

        internal async ValueTask InitAsync()
        {
            _objectStore = await _storeFactory.CreateAsync(_configPath, _bytesPool);
        }

        protected override async ValueTask OnDisposeAsync()
        {
            await _objectStore.DisposeAsync();
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

        private static void ConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
        {
            var image = Image.Load(inStream);
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

            if (formatType == ThumbnailFormatType.Png)
            {
                var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
                image.Save(outStream, encoder);
                return;
            }

            throw new NotSupportedException();
        }

        private async ValueTask<ThumbnailGeneratorGetResult> GetPictureThumnailAsync(OmniPath omniPath, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
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

            var storePath = $"/v1/picture/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/{width}x{height}_{ResizeTypeToString(resizeType)}_{FormatTypeToString(formatType)}";
            var entry = await _objectStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken);

            if (entry != ThumbnailEntity.Empty)
            {
                if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                    && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                {
                    return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Succeeded, entry.Contents);
                }
            }

            try
            {
                using (var inStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                using (var outStream = new RecyclableMemoryStream(_bytesPool))
                {
                    ConvertImage(inStream, outStream, width, height, resizeType, formatType);
                    outStream.Seek(0, SeekOrigin.Begin);

                    var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                    var content = new ThumbnailContent(outStream.ToMemoryOwner());
                    entry = new ThumbnailEntity(metadata, new[] { content });

                    await _objectStore.WriteAsync(storePath, entry, cancellationToken);
                }

                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Succeeded, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Info(e);
            }

            return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
        }

        private async ValueTask<ThumbnailGeneratorGetResult> GetMovieThumnailAsync(OmniPath omniPath, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
        {
            if (!OmniPath.Windows.TryDecoding(omniPath, out var path))
            {
                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
            }

            if (!_movieTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
            }

            var duration = await this.GetMovieDurationAsync(path, cancellationToken);
            var interval = TimeSpan.FromSeconds(Math.Max(5, duration.TotalSeconds / 30));

            var fullPath = Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);

            var storePath = $"/v1/movie/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/{interval.TotalSeconds}_{width}x{height}_{ResizeTypeToString(resizeType)}_{FormatTypeToString(formatType)}";
            var entry = await _objectStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken);

            if (entry != ThumbnailEntity.Empty)
            {
                if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                    && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                {
                    return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Succeeded, entry.Contents);
                }
            }

            try
            {
                var images = await GetMovieImagesAsync(fullPath, interval, width, height, resizeType, formatType, cancellationToken);

                var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
                entry = new ThumbnailEntity(metadata, contents);

                await _objectStore.WriteAsync(storePath, entry, cancellationToken);

                return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Succeeded, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Info(e);
            }

            return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
        }

        private async ValueTask<TimeSpan> GetMovieDurationAsync(string path, CancellationToken cancellationToken = default)
        {
            var arguments = $"-v error -select_streams v:0 -show_entries stream=duration -sexagesimal -of default=noprint_wrappers=1:nokey=1 \"{path}\"";

            using var process = Process.Start(new ProcessStartInfo("ffprobe", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
            });

            using var baseStream = process.StandardOutput.BaseStream;
            using var reader = new StreamReader(baseStream);
            var line = await reader.ReadLineAsync();

            await process.WaitForExitAsync();

            var result = TimeSpan.Parse(line!.Trim());
            return result;
        }

        private async ValueTask<IMemoryOwner<byte>[]> GetMovieImagesAsync(string path, TimeSpan interval, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var results = new List<IMemoryOwner<byte>>();

            var duration = await this.GetMovieDurationAsync(path, cancellationToken);

            foreach (var seekSec in Enumerable.Range(0, (int)(duration.TotalSeconds / interval.TotalSeconds))
                .Select(x => x * interval.TotalSeconds))
            {
                try
                {
                    var arguments = $"-loglevel error -ss {seekSec} -i \"{path}\" -vframes 1 -f image2 pipe:1";

                    using var process = Process.Start(new ProcessStartInfo("ffmpeg", arguments)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = false,
                    });

                    using var baseStream = process.StandardOutput.BaseStream;
                    using var inStream = new RecyclableMemoryStream(_bytesPool);
                    using var outStream = new RecyclableMemoryStream(_bytesPool);

                    await baseStream.CopyToAsync(inStream);
                    inStream.Seek(0, SeekOrigin.Begin);
                    ConvertImage(inStream, outStream, width, height, resizeType, formatType);

                    await process.WaitForExitAsync();

                    results.Add(outStream.ToMemoryOwner());
                }
                catch (Exception e)
                {
                    _logger.Warn(e);
                }
            }

            return results.ToArray();
        }

        public async ValueTask<ThumbnailGeneratorGetResult> GetThumbnailAsync(OmniPath omniPath, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken cancellationToken = default)
        {
            {
                var result = await this.GetPictureThumnailAsync(omniPath, width, height, resizeType, formatType, cancellationToken);

                if (result.Status == ThumbnailGeneratorGetResultStatus.Succeeded)
                {
                    return result;
                }
            }

            {
                var result = await this.GetMovieThumnailAsync(omniPath, width, height, resizeType, formatType, cancellationToken);

                if (result.Status == ThumbnailGeneratorGetResultStatus.Succeeded)
                {
                    return result;
                }
            }

            return new ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus.Failed);
        }
    }
}
