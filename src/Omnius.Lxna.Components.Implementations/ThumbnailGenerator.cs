using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using Omnius.Core;
using Omnius.Core.Extensions;
using Omnius.Core.Io;
using Omnius.Core.RocketPack;
using Omnius.Core.Serialization;
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
        private readonly uint _concurrency;
        private readonly IFileSystem _fileSystem;
        private readonly IBytesPool _bytesPool;

        private readonly ThumbnailGeneratorRepository _thumbnailGeneratorRepository;

        private static readonly HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".bmp", ".jpg", ".jpeg", ".png", ".gif", ".heic" };
        private static readonly HashSet<string> _movieTypeExtensionList = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg", ".flv" };
        private static readonly Base16 _base16 = new Base16(ConvertStringCase.Lower);

        internal sealed class ThumbnailGeneratorFactory : IThumbnailGeneratorFactory
        {
            public async ValueTask<IThumbnailGenerator> CreateAsync(ThumbnailGeneratorOptions options)
            {
                var result = new ThumbnailGenerator(options);
                await result.InitAsync();

                return result;
            }
        }

        public static IThumbnailGeneratorFactory Factory { get; } = new ThumbnailGeneratorFactory();

        internal ThumbnailGenerator(ThumbnailGeneratorOptions options)
        {
            _configPath = options.ConfigDirectoryPath ?? throw new ArgumentNullException(nameof(options.ConfigDirectoryPath));
            _concurrency = options.Concurrency;
            _fileSystem = options.FileSystem ?? throw new ArgumentNullException(nameof(options.FileSystem));
            _bytesPool = options.BytesPool ?? BytesPool.Shared;

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

        public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(NestedPath filePath, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            if (!await _fileSystem.ExistsFileAsync(filePath, cancellationToken))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            // Cache
            {
                var result = await this.GetThumbnailFromCacheAsync(filePath, options, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            if (cacheOnly)
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            // Movie
            {
                var result = await this.GetMovieThumbnailAsync(filePath, options, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            // Picture
            {
                var result = await this.GetPictureThumbnailAsync(filePath, options, cancellationToken).ConfigureAwait(false);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailFromCacheAsync(NestedPath filePath, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
        {
            var cache = await _thumbnailGeneratorRepository.ThumbnailCaches.FindOneAsync(filePath, options.Width, options.Height, options.ResizeType, options.FormatType);

            if (cache is null)
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            var fileLength = await _fileSystem.GetFileSizeAsync(filePath, cancellationToken);
            var fileLastWriteTime = await _fileSystem.GetFileLastWriteTimeAsync(filePath, cancellationToken);

            if ((ulong)fileLength != cache.FileMeta.Length
                    && Timestamp.FromDateTime(fileLastWriteTime) != cache.FileMeta.LastWriteTime)
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, cache.Contents);
        }

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetPictureThumbnailAsync(NestedPath filePath, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
        {
            var ext = filePath.GetExtension().ToLower();
            if (!_pictureTypeExtensionList.Contains(ext))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            try
            {
                var fileLength = await _fileSystem.GetFileSizeAsync(filePath, cancellationToken);
                var fileLastWriteTime = await _fileSystem.GetFileLastWriteTimeAsync(filePath, cancellationToken);

                using (var inStream = await _fileSystem.GetFileStreamAsync(filePath, cancellationToken))
                using (var outStream = new RecyclableMemoryStream(_bytesPool))
                {
                    this.ConvertImage(inStream, outStream, options.Width, options.Height, options.ResizeType, options.FormatType);
                    outStream.Seek(0, SeekOrigin.Begin);

                    var fileMeta = new FileMeta(filePath, (ulong)fileLength, Timestamp.FromDateTime(fileLastWriteTime));
                    var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                    var content = new ThumbnailContent(outStream.ToMemoryOwner());
                    var cache = new ThumbnailCache(fileMeta, thumbnailMeta, new[] { content });

                    await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);

                    return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, cache.Contents);
                }
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

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetMovieThumbnailAsync(NestedPath filePath, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
        {
            if (!_movieTypeExtensionList.Contains(filePath.GetExtension().ToLower()))
            {
                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
            }

            try
            {
                var fileLength = await _fileSystem.GetFileSizeAsync(filePath, cancellationToken);
                var fileLastWriteTime = await _fileSystem.GetFileLastWriteTimeAsync(filePath, cancellationToken);

                var images = await this.GetMovieImagesAsync(filePath, options.MinInterval, options.MaxImageCount, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken).ConfigureAwait(false);

                var fileMeta = new FileMeta(filePath, (ulong)fileLength, Timestamp.FromDateTime(fileLastWriteTime));
                var thumbnailMeta = new ThumbnailMeta(options.ResizeType, options.FormatType, (uint)options.Width, (uint)options.Height);
                var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
                var cache = new ThumbnailCache(fileMeta, thumbnailMeta, contents);

                await _thumbnailGeneratorRepository.ThumbnailCaches.InsertAsync(cache);

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

        private async ValueTask<IMemoryOwner<byte>[]> GetMovieImagesAsync(NestedPath filePath, TimeSpan minInterval, int maxImageCount, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
        {
            var resultMap = new ConcurrentDictionary<int, IMemoryOwner<byte>>();

            using var extractedFileOwner = await _fileSystem.ExtractFileAsync(filePath, cancellationToken);

            var duration = await GetMovieDurationAsync(extractedFileOwner.Path, cancellationToken).ConfigureAwait(false);
            int intervalSeconds = (int)Math.Max(minInterval.TotalSeconds, duration.TotalSeconds / maxImageCount);
            int imageCount = (int)(duration.TotalSeconds / intervalSeconds);

            await Enumerable.Range(1, imageCount)
                .Select(x => x * intervalSeconds)
                .Where(seekSec => (duration.TotalSeconds - seekSec) > 1) // 残り1秒以下の場合は除外
                .ForEachAsync(
                    async seekSec =>
                    {
                        var ret = await this.GetMovieImagesAsync(extractedFileOwner.Path, seekSec, width, height, resizeType, formatType, cancellationToken);
                        resultMap.TryAdd(ret.SeekSec, ret.MemoryOwner);
                    }, (int)_concurrency, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (!resultMap.IsEmpty)
            {
                var tempList = resultMap.ToList();
                tempList.Sort((x, y) => x.Key.CompareTo(y.Key));

                return tempList.Select(n => n.Value).ToArray();
            }

            var ret = await this.GetMovieImageAsync(extractedFileOwner.Path, width, height, resizeType, formatType, cancellationToken);
            return new[] { ret };
        }

        private async ValueTask<IMemoryOwner<byte>> GetMovieImageAsync(string path, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
        {
            try
            {
                var arguments = $"-loglevel error -i \"{path}\" -vf thumbnail=30 -frames:v 1 -f image2 pipe:1";

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
                await process.WaitForExitAsync(cancellationToken);

                this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

                return outStream.ToMemoryOwner();
            }
            catch (Exception e)
            {
                _logger.Warn(e);
                throw;
            }
        }

        private async ValueTask<(int SeekSec, IMemoryOwner<byte> MemoryOwner)> GetMovieImagesAsync(string path, int seekSec, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, CancellationToken cancellationToken = default)
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
                await process.WaitForExitAsync(cancellationToken);

                this.ConvertImage(inStream, outStream, width, height, resizeType, formatType);

                return (seekSec, outStream.ToMemoryOwner());
            }
            catch (Exception e)
            {
                _logger.Warn(e);
                throw;
            }
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

            if (process is null) throw new NotSupportedException();

            using var baseStream = process.StandardOutput.BaseStream;
            using var reader = new StreamReader(baseStream);
            var line = await reader.ReadLineAsync().ConfigureAwait(false);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (line == null || !TimeSpan.TryParse(line.Trim(), out var result)) throw new NotSupportedException();

            return result;
        }

        private void ConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
        {
            try
            {
                this.InternalImageSharpConvertImage(inStream, outStream, width, height, resizeType, formatType);
            }
            catch (SixLabors.ImageSharp.UnknownImageFormatException)
            {
                using (var bitmapStream = new RecyclableMemoryStream(_bytesPool))
                {
                    this.InternalMagickImageConvertImage(inStream, bitmapStream);
                    bitmapStream.Seek(0, SeekOrigin.Begin);

                    this.InternalImageSharpConvertImage(bitmapStream, outStream, width, height, resizeType, formatType);
                }
            }
        }

        private void InternalMagickImageConvertImage(Stream inStream, Stream outStream)
        {
            try
            {
                using var magickImage = new MagickImage(inStream, MagickFormat.Unknown);
                magickImage.Write(outStream, MagickFormat.Png32);
            }
            catch (Exception e)
            {
                throw new NotSupportedException(e.GetType().ToString(), e);
            }
        }

        private void InternalImageSharpConvertImage(Stream inStream, Stream outStream, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
        {
            using var image = SixLabors.ImageSharp.Image.Load(inStream);
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
}
