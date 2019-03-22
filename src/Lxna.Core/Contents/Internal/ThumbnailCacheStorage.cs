using System;
using System.Collections.Generic;
using System.Text;
using Omnix.Base;
using Omnix.Configuration;
using Lxna.Messages;
using System.IO;
using LiteDB;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using Omnix.Io;
using System.Buffers;
using Lxna.Core.Internal;
using Omnix.Serialization;

namespace Lxna.Core.Contents.Internal
{
    public sealed class ThumbnailCacheStorage : DisposableBase
    {
        private readonly string _basePath;
        private LiteDatabase _liteDatabase;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private volatile bool _disposed;

        private readonly static HashSet<string> _pictureTypeExtensionList = new HashSet<string>() { ".jpg", ".png", ".gif" };
        private readonly static HashSet<string> _movieTypeExtensionList = new HashSet<string>() { ".mp4", ".avi" };

        public ThumbnailCacheStorage(string basePath)
        {
            _basePath = basePath;
            _liteDatabase = new LiteDatabase(Path.Combine(_basePath, "Thumbnail.db"));
        }

        private static bool ExtensionIsPictureType(string ext)
        {
            return _pictureTypeExtensionList.Contains(ext);
        }

        public IEnumerable<ThumbnailImage> GetThumnailImages(string path, int width, int height)
        {
            var fullPath = Path.GetFullPath(path);
            var fileInfo = new FileInfo(fullPath);

            var databaseId = fullPath + $"{height}x{width}";
            var thumbnailId = new ThumbnailId(path, Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc), (ulong)fileInfo.Length);

            ThumbnailInfo thumbnailInfo = null;

            // データベースからThumbnailInfoの読み込みを行う。
            {
                var liteFileInfo = _liteDatabase.FileStorage.FindById(databaseId);

                if (liteFileInfo != null)
                {
                    using (var inStream = liteFileInfo.OpenRead())
                    {
                        thumbnailInfo = RocketPackHelper.StreamToMessage<ThumbnailInfo>(inStream);
                    }
                }
            }

            // ThumbnailInfoを読み込めた場合には、FileIdのチェックを行う。
            if (thumbnailInfo?.Id != thumbnailId)
            {
                _liteDatabase.FileStorage.Delete(databaseId);

                foreach (var thumbnailImage in thumbnailInfo.Images)
                {
                    thumbnailImage.Dispose();
                }

                thumbnailInfo = null;
            }

            if (thumbnailInfo == null)
            {
                IMemoryOwner<byte> memoryOwner = null;

                // 画像を読み込み、リサイズを行う。
                using (var image = Image.Load(fullPath))
                {
                    image.Mutate(x =>
                    {
                        x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Crop,
                            Position = AnchorPositionMode.Center,
                            Size = new SixLabors.Primitives.Size(width, height)
                        });
                    });

                    using (var stream = new RecyclableMemoryStream(BufferPool.Shared))
                    {
                        var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
                        image.Save(stream, encoder);
                        memoryOwner = stream.ToArray();
                    }
                }

                thumbnailInfo = new ThumbnailInfo(thumbnailId, new[] { new ThumbnailImage(ImageFormatType.Png, memoryOwner) });

                // データベースに画像を保存する。
                using (var recyclableMemoryStream = new RecyclableMemoryStream(BufferPool.Shared))
                {
                    RocketPackHelper.MessageToStream(thumbnailInfo, recyclableMemoryStream);
                    recyclableMemoryStream.Seek(0, SeekOrigin.Begin);

                    _liteDatabase.FileStorage.Upload(databaseId, Path.GetFileName(fullPath), recyclableMemoryStream);
                }
            }

            return thumbnailInfo.Images;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                if (_liteDatabase != null)
                {
                    _liteDatabase.Dispose();
                    _liteDatabase = null;
                }
            }
        }
    }
}
