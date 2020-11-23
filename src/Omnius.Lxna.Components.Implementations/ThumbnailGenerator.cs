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
using Omnius.Core;
using Omnius.Core.Cryptography.Functions;
using Omnius.Core.Extensions;
using Omnius.Core.Io;
using Omnius.Core.RocketPack;
using Omnius.Core.Serialization;
using Omnius.Core.Serialization.Extensions;
using Omnius.Lxna.Components.Internal.Models;
using Omnius.Lxna.Components.Internal.Repositories;
using Omnius.Lxna.Components.Models;
using SixLabors.ImageSharp.Processing;

namespace Omnius.Lxna.Components
{
    public sealed class ThumbnailGenerator : AsyncDisposableBase, IThumbnailGenerator
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _configPath;
        private readonly ThumbnailGeneratorOptions _options;
        private readonly IBytesPool _bytesPool;

        private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;

        private static readonly HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".heic" };
        private static readonly HashSet<string> _movieTypeExtensionList = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg", "flv" };
        private static readonly Base16 _base16 = new Base16(ConvertStringCase.Lower);

        internal sealed class ThumbnailGeneratorFactory : IThumbnailGeneratorFactory
        {
            public async ValueTask<IThumbnailGenerator> CreateAsync(string configPath, ThumbnailGeneratorOptions options, IBytesPool bytesPool)
            {
                var result = new ThumbnailGenerator(configPath, options, bytesPool);
                await result.InitAsync();

                return result;
            }
        }

        public static IThumbnailGeneratorFactory Factory { get; } = new ThumbnailGeneratorFactory();

        internal ThumbnailGenerator(string configPath, ThumbnailGeneratorOptions options, IBytesPool bytesPool)
        {
            _configPath = configPath;
            _options = options;
            _bytesPool = bytesPool;

            _thumbnailGeneratorRepository = new ThumbnailGeneratorRepository(Path.Combine(_configPath, "thumbnails.db"), _bytesPool);
        }

        internal async ValueTask InitAsync()
        {
            await _thumbnailGeneratorRepository.MigrateAsync();
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _thumbnailGeneratorRepository.Dispose();
        }

        public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(string filePath, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            // Picture
            {
                var result = await this.GetPictureThumbnailAsync(filePath, options, cacheOnly, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            // Movie
            {
                var result = await this.GetMovieThumbnailAsync(filePath, options, cacheOnly, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }

        private void ConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
        {
            using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
            {
                try
                {
                    using (var magickImage = new ImageMagick.MagickImage(inStream, ImageMagick.MagickFormat.Unknown))
                    {
                        magickImage.Format = ImageMagick.MagickFormat.Bmp;
                        magickImage.Write(bitmapStream);
                    }

                    bitmapStream.Seek(0, SeekOrigin.Begin);
                }
                catch (Exception e)
                {
                    // MagickImageはExceptionを返すことがある
                    throw new NotSupportedException(e.GetType().ToString(), e);
                }

                var image = SixLabors.ImageSharp.Image.Load(bitmapStream);
                image.Mutate(x =>
                {
                    var resizeOptions = new ResizeOptions
                    {
                        Position = AnchorPositionMode.Center,
                        Size = new SixLabors.ImageSharp.Size(width, height),
                        Mode = resizeType switch
                        {
                            ThumbnailResizeType.Pad => ResizeMode.Pad,
                            ThumbnailResizeType.Crop => ResizeMode.Crop,
                            _ => throw new NotSupportedException(),
                        },
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

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetPictureThumbnailAsync(string filePath, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            if (!_pictureTypeExtensionList.Contains(Path.GetExtension(filePath).ToLower()))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var fileInfo = new FileInfo(fullPath);

                var cache = await _thumbnailGeneratorRepository.ThumbnailCaches.FindOneAsync(fullPath, options.Width, options.Height, options.ResizeType, options.FormatType);

                if (cache is not null)
                {
                    if ((ulong)fileInfo.Length == cache.FileMeta.Length
                        && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == cache.FileMeta.LastWriteTime)
                    {
                        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, cache.Contents);
                    }
                }

                if (cacheOnly)
                {
                    return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
                }

                using (var inStream = new FileStream(fullPath, FileMode.Open))
                using (var outStream = new RecyclableMemoryStream(_bytesPool))
                {
                    this.ConvertImage(inStream, outStream, options.Width, options.Height, options.ResizeType, options.FormatType);
                    outStream.Seek(0, SeekOrigin.Begin);

                    var fileMeta = new FileMeta(fullPath, (ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                    var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                    var content = new ThumbnailContent(outStream.ToMemoryOwner());
                    cache = new ThumbnailCache(fileMeta, thumbnailMeta, new[] { content });

                    await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);
                }

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, cache.Contents);
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
                throw;
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }

        private static async ValueTask<TimeSpan> GetMovieDurationAsync(string path, CancellationToken cancellationToken = default)
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

            var duration = await GetMovieDurationAsync(path, cancellationToken).ConfigureAwait(false);
            int intervalSeconds = (int)Math.Max(minInterval.TotalSeconds, duration.TotalSeconds / maxImageCount);
            int imageCount = (int)(duration.TotalSeconds / intervalSeconds);

            await Enumerable.Range(1, imageCount)
                .Select(x => x * intervalSeconds)
                .Where(seekSec => (duration.TotalSeconds - seekSec) > 1) // 残り1秒以下の場合は除外
                .ForEachAsync(
                    async (seekSec) =>
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

                            using var baseStream = process!.StandardOutput.BaseStream;
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
                            throw;
                        }
                    }, (int)_options.Concurrency, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (resultMap.IsEmpty)
            {
                throw new NotSupportedException();
            }

            var tempList = resultMap.ToList();
            tempList.Sort((x, y) => x.Key.CompareTo(y.Key));

            return tempList.Select(n => n.Value).ToArray();
        }

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetMovieThumbnailAsync(string filePath, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            if (!_movieTypeExtensionList.Contains(Path.GetExtension(filePath).ToLower()))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            try
            {
                var fullPath = Path.GetFullPath(filePath);
                var fileInfo = new FileInfo(fullPath);

                var cache = await _thumbnailGeneratorRepository.ThumbnailCaches.FindOneAsync(fullPath, options.Width, options.Height, options.ResizeType, options.FormatType);

                if (cache is not null)
                {
                    if ((ulong)fileInfo.Length == cache.FileMeta.Length
                        && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == cache.FileMeta.LastWriteTime)
                    {
                        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, cache.Contents);
                    }
                }

                if (cacheOnly)
                {
                    return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
                }

                var images = await this.GetMovieImagesAsync(fullPath, options.MinInterval, options.MaxImageCount, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken).ConfigureAwait(false);

                var fileMeta = new FileMeta(fullPath, (ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
                cache = new ThumbnailCache(fileMeta, thumbnailMeta, contents);

                await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);
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
                throw;
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }
    }
}
