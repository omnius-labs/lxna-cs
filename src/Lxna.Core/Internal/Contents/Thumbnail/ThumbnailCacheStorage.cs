using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LiteDB;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Cryptography;
using Omnix.Io;
using Omnix.Network;
using Omnix.Serialization;
using Omnix.Serialization.Extensions;
using Omnix.Serialization.RocketPack;
using Omnix.Serialization.RocketPack.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Lxna.Core.Internal.Contents.Thumbnail
{
    public sealed class ThumbnailCacheStorage : DisposableBase
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LxnaOptions _options;
        private readonly LiteDatabase _liteDatabase;

        private volatile bool _disposed;

        private readonly static HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly static HashSet<string> _videoTypeExtensionList = new HashSet<string>() { ".mp4", ".avi" };
        private readonly static Base16 _base16 = new Base16(ConvertStringCase.Lower);

        public ThumbnailCacheStorage(LxnaOptions options)
        {
            _options = options;

            var directoryPath = Path.Combine(_options.ConfigDirectoryPath, "ThumbnailCache");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            _liteDatabase = new LiteDatabase(Path.Combine(directoryPath, "lite.db"));
        }

        private static string ResizeTypeToString(LxnaThumbnailResizeType resizeType)
        {
            return resizeType switch
            {
                LxnaThumbnailResizeType.Crop => "crop",
                LxnaThumbnailResizeType.Pad => "pad",
                _ => throw new NotSupportedException(nameof(resizeType)),
            };
        }

        private static string FormatTypeToString(LxnaThumbnailFormatType formatType)
        {
            return formatType switch
            {
                LxnaThumbnailFormatType.Png => "png",
                _ => throw new NotSupportedException(nameof(formatType)),
            };
        }

        private LxnaThumbnail? GetPictureThumnail(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            if (!OmniAddress.Windows.FileSystem.TryDecoding(address, out var path, out int _))
            {
                throw new ArgumentException();
            }

            if (!_pictureTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return null;
            }

            var fullPath = Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);

            var databaseId = $"$/picture/v1_{FormatTypeToString(formatType)}_{width}x{height}_{ResizeTypeToString(resizeType)}/{_base16.BytesToString(Sha2_256.ComputeHash(fullPath))}/";
            var fileId = new FileId(address, (ulong)fileInfo.Length, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc));

            {
                bool changed = true;

                // DBに保存されているFileIdと比較を行い、サムネイル生成元のファイルが更新されているかを確認する。
                {
                    var liteFileInfo = _liteDatabase.FileStorage.FindById(databaseId + "FileId");

                    if (liteFileInfo != null)
                    {
                        using (var inStream = liteFileInfo.OpenRead())
                        {
                            var readFileId = RocketPackHelper.StreamToMessage<FileId>(inStream);

                            if(fileId == readFileId)
                            {
                                changed = false;
                            }
                        }
                    }
                }

                // キャッシュされたサムネイルが存在し、サムネイル生成元のファイルが更新されていない場合。
                if (!changed)
                {
                    var liteFileInfo = _liteDatabase.FileStorage.FindById(databaseId + "ThumbnailCache");

                    if (liteFileInfo != null)
                    {
                        using (var inStream = liteFileInfo.OpenRead())
                        {
                            var readThumbnailCache = RocketPackHelper.StreamToMessage<ThumbnailsCache>(inStream);

                            if (readThumbnailCache != null)
                            {
                                // キャッシュされているサムネイルを返す。
                                return readThumbnailCache.Thumbnails.First();
                            }
                        }
                    }
                }

                // サムネイルのキャッシュが存在しない、またはサムネイル生成元ファイルが更新されている場合、サムネイル画像を生成する。
                {
                    LxnaThumbnail? thumbnail = null;

                    try
                    {
                        // 画像を読み込み、リサイズを行う。
                        using (var image = Image.Load(fullPath))
                        {
                            image.Mutate(x =>
                            {
                                var resizeOptions = new ResizeOptions();
                                resizeOptions.Position = AnchorPositionMode.Center;
                                resizeOptions.Size = new SixLabors.Primitives.Size(width, height);
                                resizeOptions.Mode = resizeType switch
                                {
                                    LxnaThumbnailResizeType.Pad => ResizeMode.Pad,
                                    LxnaThumbnailResizeType.Crop => ResizeMode.Crop,
                                    _ => throw new NotSupportedException(),
                                };

                                x.Resize(resizeOptions);
                            });

                            using (var stream = new RecyclableMemoryStream(BufferPool.Shared))
                            {
                                var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
                                image.Save(stream, encoder);
                                thumbnail = new LxnaThumbnail(stream.ToMemoryOwner());
                            }
                        }
                    }
                    catch (NotSupportedException e)
                    {
                        _logger.Info(e);
                    }

                    if (thumbnail == null)
                    {
                        return null;
                    }

                    var thumbnailCache = new ThumbnailsCache(new[] { thumbnail });

                    // データベースにFileIdを保存する。
                    using (var recyclableMemoryStream = new RecyclableMemoryStream(BufferPool.Shared))
                    {
                        RocketPackHelper.MessageToStream(fileId, recyclableMemoryStream);
                        recyclableMemoryStream.Seek(0, SeekOrigin.Begin);

                        _liteDatabase.FileStorage.Upload(databaseId + "FileId", Path.GetFileName(fullPath), recyclableMemoryStream);
                    }

                    // データベースにThumbnailCacheを保存する。
                    using (var recyclableMemoryStream = new RecyclableMemoryStream(BufferPool.Shared))
                    {
                        RocketPackHelper.MessageToStream(thumbnailCache, recyclableMemoryStream);
                        recyclableMemoryStream.Seek(0, SeekOrigin.Begin);

                        _liteDatabase.FileStorage.Upload(databaseId + "ThumbnailCache", Path.GetFileName(fullPath), recyclableMemoryStream);
                    }

                    return thumbnail;
                }
            }
        }

        private IEnumerable<LxnaThumbnail>? GetVideoThumnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            if (!OmniAddress.Windows.FileSystem.TryDecoding(address, out var path, out int _))
            {
                throw new ArgumentException();
            }

            if (!_videoTypeExtensionList.Contains(Path.GetExtension(path).ToLower()))
            {
                return null;
            }

            // TODO
            return null;
        }

        public IEnumerable<LxnaThumbnail> GetThumnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            {
                var result = this.GetPictureThumnail(address, width, height, formatType, resizeType, token);

                if (result != null)
                {
                    return new[] { result };
                }
            }

            {
                var result = this.GetVideoThumnails(address, width, height, formatType, resizeType, token);

                if (result != null)
                {
                    return result;
                }
            }

            return Enumerable.Empty<LxnaThumbnail>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _liteDatabase.Dispose();
            }
        }
    }
}
