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
        private readonly string _basePath;

        private readonly ContentsManager _contentsManager;

        private ServiceStateType _state = ServiceStateType.Stopped;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private volatile bool _disposed;

        public LxnaService(string basePath)
        {
            _basePath = basePath;

            _contentsManager = new ContentsManager(basePath);
        }

        public IEnumerable<FileMetadata> GetFileMetadatas(string path)
        {
            foreach (var fileInfo in new DirectoryInfo(path).GetFiles())
            {
                yield return new FileMetadata(fileInfo.Name);
            }
        }

        public IEnumerable<ThumbnailImage> GetFileThumbnail(string path, int width, int height)
        {
            return _contentsManager.GetThumnailImages(path, width, height);
        }

        public void ReadFileContent(string path, long position, Span<byte> buffer)
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

        public override async ValueTask Start(CancellationToken token = default)
        {
            using (await _asyncLock.LockAsync())
            {
                this.InternalStart();
            }
        }

        public override async ValueTask Stop(CancellationToken token = default)
        {
            using (await _asyncLock.LockAsync())
            {
                this.InternalStop();
            }
        }

        public override async ValueTask Restart(CancellationToken token = default)
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

            }
        }
    }
}
