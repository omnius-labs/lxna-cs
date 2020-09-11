using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Omnius.Core;
using Omnius.Core.Cryptography;
using Omnius.Core.Extensions;
using Omnius.Core.Io;
using Omnius.Core.Network;
using Omnius.Core.Serialization;
using Omnius.Core.Serialization.Extensions;
using Omnius.Core.Serialization.RocketPack;
using Omnius.Lxna.Components.Internal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components
{
    public sealed class ThumbnailGenerator : AsyncDisposableBase, IThumbnailGenerator
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _configPath;
        private readonly IObjectStoreFactory _objectStoreFactory;
        private readonly IBytesPool _bytesPool;

        private IObjectStore _objectStore;

        private readonly static HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".heic" };
        private readonly static HashSet<string> _movieTypeExtensionList = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg", "flv" };
        private readonly static Base16 _base16 = new Base16(ConvertStringCase.Lower);

        private readonly int _concurrency = 8;

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

        internal ThumbnailGenerator(string configPath, IObjectStoreFactory objectStoreFactory, IBytesPool bytesPool)
        {
            _configPath = configPath;
            _objectStoreFactory = objectStoreFactory;
            _bytesPool = bytesPool;
        }

        internal async ValueTask InitAsync()
        {
            _objectStore = await _objectStoreFactory.CreateAsync(_configPath, _bytesPool);
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

        private void ConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
        {
            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                try
                {
                    using (var magickImage = new MagickImage(inStream, MagickFormat.Unknown))
                    {
                        magickImage.Format = MagickFormat.Bmp;
                        magickImage.Write(bitmapStream);
                    }

                    bitmapStream.Seek(0, SeekOrigin.Begin);
                }
                catch (Exception e) // MagickImageはExceptionを返すことがある
                {
                    throw new NotSupportedException(e.GetType().ToString(), e);
                }

                var image = Image.Load(bitmapStream);
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
        }

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetPictureThumnailAsync(OmniPath omniPath, ThumbnailGeneratorGetThumbnailOptions options, bool fromCache, CancellationToken cancellationToken = default)
        {
            if (!OmniPath.Windows.TryDecoding(omniPath, out var path))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            if (!_pictureTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                var fileInfo = new FileInfo(fullPath);

                var storePath = $"/v1/picture/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/{options.Width}x{options.Height}_{ResizeTypeToString(options.ResizeType)}_{FormatTypeToString(options.FormatType)}";
                var entry = await _objectStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken).ConfigureAwait(false);

                if (entry != ThumbnailEntity.Empty)
                {
                    if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                        && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                    {
                        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
                    }
                }

                if (fromCache)
                {
                    return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
                }

                using (var inStream = new FileStream(omniPath.ToCurrentPlatformPath(), FileMode.Open))
                using (var outStream = new RecyclableMemoryStream(_bytesPool))
                {
                    this.ConvertImage(inStream, outStream, options.Width, options.Height, options.ResizeType, options.FormatType);
                    outStream.Seek(0, SeekOrigin.Begin);

                    var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                    var content = new ThumbnailContent(outStream.ToMemoryOwner());
                    entry = new ThumbnailEntity(metadata, new[] { content });

                    await _objectStore.WriteAsync(storePath, entry, cancellationToken).ConfigureAwait(false);
                }

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Warn(e);
            }
            catch (OperationCanceledException e)
            {
                _logger.Debug(e);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw e;
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
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
            var line = await reader.ReadLineAsync().ConfigureAwait(false);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (line == null || !TimeSpan.TryParse(line.Trim(), out var result))
            {
                throw new NotSupportedException();
            }

            return result;
        }

        private async ValueTask<IMemoryOwner<byte>[]> GetMovieImagesAsync(string path, TimeSpan minInterval, int maxImageCount, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var resultMap = new ConcurrentDictionary<int, IMemoryOwner<byte>>();

            var duration = await this.GetMovieDurationAsync(path, cancellationToken).ConfigureAwait(false);
            int intervalSeconds = (int)Math.Max(minInterval.TotalSeconds, duration.TotalSeconds / maxImageCount);
            int imageCount = (int)(duration.TotalSeconds / intervalSeconds);

            await Enumerable.Range(1, imageCount)
                .Select(x => x * intervalSeconds)
                .Where(seekSec => (duration.TotalSeconds - seekSec) > 1) // 残り1秒以下の場合は除外
                .ForEachAsync(async (seekSec) =>
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

                        await baseStream.CopyToAsync(inStream, cancellationToken);
                        inStream.Seek(0, SeekOrigin.Begin);
                        this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

                        await process.WaitForExitAsync(cancellationToken);

                        resultMap[seekSec] = outStream.ToMemoryOwner();
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e);
                        throw e;
                    }
                }, _concurrency, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (resultMap.Count == 0)
            {
                throw new NotSupportedException();
            }

            var tempList = resultMap.ToList();
            tempList.Sort((x, y) => x.Key.CompareTo(y.Key));

            return tempList.Select(n => n.Value).ToArray();
        }

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetMovieThumnailAsync(OmniPath omniPath, ThumbnailGeneratorGetThumbnailOptions options, bool fromCache, CancellationToken cancellationToken = default)
        {
            if (!OmniPath.Windows.TryDecoding(omniPath, out var path))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            if (!_movieTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                var fileInfo = new FileInfo(fullPath);

                var storePath = $"/v1/movie/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/{(int)options.MinInterval.TotalSeconds}_{options.MaxImageCount}_{options.Width}x{options.Height}_{ResizeTypeToString(options.ResizeType)}_{FormatTypeToString(options.FormatType)}";
                var entry = await _objectStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken).ConfigureAwait(false);

                if (entry != ThumbnailEntity.Empty)
                {
                    if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                        && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                    {
                        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
                    }
                }

                if (fromCache)
                {
                    return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
                }

                var images = await this.GetMovieImagesAsync(fullPath, options.MinInterval, options.MaxImageCount, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken).ConfigureAwait(false);

                var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
                entry = new ThumbnailEntity(metadata, contents);

                await _objectStore.WriteAsync(storePath, entry, cancellationToken).ConfigureAwait(false);

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Warn(e);
            }
            catch (OperationCanceledException e)
            {
                _logger.Debug(e);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw e;
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }

        public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(OmniPath omniPath, ThumbnailGeneratorGetThumbnailOptions options, bool fromCache = false, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1).ConfigureAwait(false);

            {
                var result = await this.GetPictureThumnailAsync(omniPath, options, fromCache, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            {
                var result = await this.GetMovieThumnailAsync(omniPath, options, fromCache, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }
    }
}
