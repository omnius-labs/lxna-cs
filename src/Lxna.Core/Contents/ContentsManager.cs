using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Core.Contents.Internal;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Configuration;

namespace Lxna.Core.Contents
{
    public sealed class ContentsManager : ServiceBase, ISettings
    {
        private readonly string _basePath;

        private readonly ThumbnailCacheStorage _thumbnailCacheStorage;

        private ServiceStateType _state = ServiceStateType.Stopped;

        private readonly AsyncLock _asyncLock = new AsyncLock();
        private volatile bool _disposed;

        public ContentsManager(string basePath)
        {
            _basePath = basePath;

            _thumbnailCacheStorage = new ThumbnailCacheStorage(_basePath);
        }

        public override ServiceStateType StateType { get; }

        public IEnumerable<ThumbnailImage> GetThumnailImages(string path, int width, int height)
        {
            return _thumbnailCacheStorage.GetThumnailImages(path, width, height);
        }

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
