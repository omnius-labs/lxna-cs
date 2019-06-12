using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Core.Contents.Internal;
using Lxna.Messages;
using Lxna.Rpc.Primitives;
using Omnix.Base;
using Omnix.Network;

namespace Lxna.Core.Contents
{
    public sealed class ContentExplorer : ServiceBase, ISettings
    {
        private readonly LxnaOptions _options;
        private readonly ThumbnailCacheStorage _thumbnailCacheStorage;

        private ServiceStateType _state = ServiceStateType.Stopped;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private volatile bool _disposed;

        public ContentExplorer(LxnaOptions options)
        {
            _options = options;
            _thumbnailCacheStorage = new ThumbnailCacheStorage(_options);
        }

        public IEnumerable<LxnaThumbnail> GetThumnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            return _thumbnailCacheStorage.GetThumnailImages(address, width, height, formatType, resizeType, token);
        }

        public IEnumerable<LxnaContentId> GetContentIds(OmniAddress? baseAddress, CancellationToken token = default)
        {
            var result = new List<LxnaContentId>();

            if (baseAddress is null)
            {
                foreach (var drivePath in Directory.GetLogicalDrives())
                {
                    if (FileSystemPathConverter.TryEncoding(drivePath, out var driveAddress))
                    {
                        result.Add(new LxnaContentId(LxnaContentType.Directory, driveAddress));
                    }
                }
            }
            else
            {
                if (FileSystemPathConverter.TryDecoding(baseAddress, out var basePath))
                {
                    try
                    {
                        foreach (var directoryPath in Directory.EnumerateDirectories(basePath, "*", SearchOption.TopDirectoryOnly))
                        {
                            if (FileSystemPathConverter.TryEncoding(directoryPath, out var directoryAddress))
                            {
                                result.Add(new LxnaContentId(LxnaContentType.Directory, directoryAddress));
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }

                    try
                    {
                        foreach (var filePath in Directory.EnumerateFiles(basePath, "*", SearchOption.TopDirectoryOnly))
                        {
                            if (FileSystemPathConverter.TryEncoding(filePath, out var fileAddress))
                            {
                                result.Add(new LxnaContentId(LxnaContentType.File, fileAddress));
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }
                }
            }

            return result;
        }

        public void ReadContent(OmniAddress address, long position, Span<byte> buffer, CancellationToken token = default)
        {
            if (!FileSystemPathConverter.TryDecoding(address, out var path))
            {
                throw new ArgumentException();
            }

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Read(buffer);
            }
        }

        public void Load()
        {
        }

        public void Save()
        {
        }

        public override ServiceStateType StateType { get; }

        internal void InternalStart()
        {
            if (this.StateType != ServiceStateType.Stopped)
            {
                return;
            }

            _state = ServiceStateType.Starting;

            _state = ServiceStateType.Running;
        }

        internal void InternalStop()
        {
            if (this.StateType != ServiceStateType.Running)
            {
                return;
            }

            _state = ServiceStateType.Stopping;

            _state = ServiceStateType.Stopped;
        }

        public override async ValueTask Start()
        {
            using (await _asyncLock.LockAsync())
            {
                this.InternalStart();
            }
        }

        public override async ValueTask Stop()
        {
            using (await _asyncLock.LockAsync())
            {
                this.InternalStop();
            }
        }

        public override async ValueTask Restart()
        {
            using (await _asyncLock.LockAsync())
            {
                this.InternalStop();
                this.InternalStart();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
               
            }
        }
    }
}
