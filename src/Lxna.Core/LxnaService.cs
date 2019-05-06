using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Core.Contents;
using Lxna.Messages;
using Lxna.Rpc;
using Omnix.Base;
using Omnix.Configuration;

namespace Lxna.Core
{
    public sealed class LxnaService : ServiceBase, ILxnaService, ISettings
    {
        private readonly LxnaOptions _options;
        private readonly ContentsManager _contentsManager;

        private ServiceStateType _state = ServiceStateType.Stopped;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private volatile bool _disposed;

        public LxnaService(LxnaOptions options)
        {
            _options = options;
            _contentsManager = new ContentsManager(_options);
        }

        public IEnumerable<ContentId> GetContentIds(string? path, CancellationToken token = default)
        {
            var result = new List<ContentId>();

            if (path is null)
            {
                foreach (var drivePath in Directory.GetLogicalDrives())
                {
                    result.Add(new ContentId(ContentType.Directory, drivePath));
                }
            }
            else
            {
                try
                {
                    foreach (var directoryPath in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
                    {
                        result.Add(new ContentId(ContentType.Directory, directoryPath));
                    }
                }
                catch (UnauthorizedAccessException)
                {

                }

                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
                    {
                        result.Add(new ContentId(ContentType.File, filePath));
                    }
                }
                catch (UnauthorizedAccessException)
                {

                }
            }

            return result;
        }

        public IEnumerable<Thumbnail> GetThumbnails(string path, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken token = default)
        {
            return _contentsManager.GetThumnails(path, width, height, formatType, resizeType, token);
        }

        public void ReadContent(string path, long position, Span<byte> buffer, CancellationToken token = default)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Read(buffer);
            }
        }

        public override ServiceStateType StateType { get; }

        public void Load()
        {
        }

        public void Save()
        {
        }

        internal void InternalStart()
        {
            if (this.StateType != ServiceStateType.Stopped) return;
            _state = ServiceStateType.Starting;

            _state = ServiceStateType.Running;
        }

        internal void InternalStop()
        {
            if (this.StateType != ServiceStateType.Running) return;
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
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {
                _contentsManager.Dispose();
            }
        }
    }
}
