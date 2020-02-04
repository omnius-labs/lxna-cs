using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Core.Internal.Contents.Thumbnail;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Network;

namespace Lxna.Core.Internal.Contents
{
    public sealed class ContentsExplorer : DisposableBase
    {
        private readonly LxnaOptions _options;
        private readonly ThumbnailCacheStorage _thumbnailCacheStorage;

        public ContentsExplorer(LxnaOptions options)
        {
            _options = options;
            _thumbnailCacheStorage = new ThumbnailCacheStorage(_options);
        }

        public IEnumerable<LxnaContentId> GetContentIds(OmniAddress? baseAddress, CancellationToken token = default)
        {
            var result = new List<LxnaContentId>();

            if (baseAddress is null)
            {
                foreach (var drivePath in Directory.GetLogicalDrives())
                {
                    result.Add(new LxnaContentId(LxnaContentType.Directory, drivePath[0].ToString()));
                }
            }
            else
            {
                if (OmniAddress.Windows.FileSystem.TryDecoding(baseAddress, out var basePath, out int _))
                {
                    try
                    {
                        foreach (var directoryPath in Directory.EnumerateDirectories(basePath, "*", SearchOption.TopDirectoryOnly))
                        {
                            result.Add(new LxnaContentId(LxnaContentType.Directory, Path.GetFileName(directoryPath)));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }

                    try
                    {
                        foreach (var filePath in Directory.EnumerateFiles(basePath, "*", SearchOption.TopDirectoryOnly))
                        {
                            result.Add(new LxnaContentId(LxnaContentType.File, Path.GetFileName(filePath)));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }
                }
            }

            return result;
        }

        public IEnumerable<LxnaThumbnail> GetThumnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            return _thumbnailCacheStorage.GetThumnails(address, width, height, formatType, resizeType, token);
        }

        public int FileRead(OmniAddress address, long position, Span<byte> buffer, CancellationToken token = default)
        {
            if (!OmniAddress.Windows.FileSystem.TryDecoding(address, out var path, out int _))
            {
                throw new ArgumentException();
            }

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                return fileStream.Read(buffer);
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _thumbnailCacheStorage.Dispose();
            }
        }
    }
}
