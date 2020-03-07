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
        private readonly static HashSet<string> _movieTypeExtensionList = new HashSet<string>() { ".mp4", ".avi", ".wmv", ".mov", ".m4v", ".mkv", ".mpg" };
        private readonly static Base16 _base16 = new Base16(ConvertStringCase.Lower);

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

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetPictureThumnailAsync(OmniPath omniPath, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
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
                var entry = await _objectStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken);

                if (entry != ThumbnailEntity.Empty)
                {
                    if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                        && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                    {
                        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
                    }
                }

                using (var inStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                using (var outStream = new RecyclableMemoryStream(_bytesPool))
                {
                    ConvertImage(inStream, outStream, options.Width, options.Height, options.ResizeType, options.FormatType);
                    outStream.Seek(0, SeekOrigin.Begin);

                    var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                    var content = new ThumbnailContent(outStream.ToMemoryOwner());
                    entry = new ThumbnailEntity(metadata, new[] { content });

                    await _objectStore.WriteAsync(storePath, entry, cancellationToken);
                }

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Warn(e);
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }

        private async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetMovieThumnailAsync(OmniPath omniPath, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
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
                var duration = await this.GetMovieDurationAsync(path, cancellationToken);
                var intervalSeconds = (int)Math.Max(options.MinInterval.TotalSeconds, (duration.TotalSeconds / options.MaxImageCount));

                var fullPath = Path.GetFullPath(path);
                var fileInfo = new FileInfo(fullPath);

                var storePath = $"/v1/movie/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/{intervalSeconds}_{options.Width}x{options.Height}_{ResizeTypeToString(options.ResizeType)}_{FormatTypeToString(options.FormatType)}";
                var entry = await _objectStore.ReadAsync<ThumbnailEntity>(storePath, cancellationToken);

                if (entry != ThumbnailEntity.Empty)
                {
                    if ((ulong)fileInfo.Length == entry.Metadata.FileLength
                        && Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc) == entry.Metadata.FileLastWriteTime)
                    {
                        return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
                    }
                }

                var images = await this.GetMovieImagesAsync(fullPath, intervalSeconds, options.Width, options.Height, options.ResizeType, options.FormatType, cancellationToken);

                var metadata = new ThumbnailMetadata((ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));
                var contents = images.Select(n => new ThumbnailContent(n)).ToArray();
                entry = new ThumbnailEntity(metadata, contents);

                await _objectStore.WriteAsync(storePath, entry, cancellationToken);

                return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Succeeded, entry.Contents);
            }
            catch (NotSupportedException e)
            {
                _logger.Warn(e);
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
            var line = await reader.ReadLineAsync();

            await process.WaitForExitAsync(cancellationToken);

            if (!TimeSpan.TryParse(line!.Trim(), out var result))
            {
                throw new NotSupportedException();
            }

            return result;
        }

        private async ValueTask<IMemoryOwner<byte>[]> GetMovieImagesAsync(string path, int intervalSeconds, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var results = new List<IMemoryOwner<byte>>();

            var duration = await this.GetMovieDurationAsync(path, cancellationToken);

            await Enumerable.Range(0, (int)(duration.TotalSeconds / intervalSeconds)).Select(x => x * intervalSeconds)
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

                        await baseStream.CopyToAsync(inStream);
                        inStream.Seek(0, SeekOrigin.Begin);
                        ConvertImage(inStream, outStream, width, height, resizeType, formatType);

                        await process.WaitForExitAsync(cancellationToken);

                        results.Add(outStream.ToMemoryOwner());
                    }
                    catch (Exception e)
                    {
                        _logger.Warn(e);
                        throw e;
                    }
                }, 4, cancellationToken);

            if (results.Count == 0)
            {
                throw new NotSupportedException();
            }

            return results.ToArray();
        }

        public async ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(OmniPath omniPath, ThumbnailGeneratorGetThumbnailOptions options, CancellationToken cancellationToken = default)
        {
            {
                var result = await this.GetPictureThumnailAsync(omniPath, options, cancellationToken);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            {
                var result = await this.GetMovieThumnailAsync(omniPath, options, cancellationToken);

                if (result.Status == ThumbnailGeneratorResultStatus.Succeeded)
                {
                    return result;
                }
            }

            return new ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus.Failed);
        }
    }
}
