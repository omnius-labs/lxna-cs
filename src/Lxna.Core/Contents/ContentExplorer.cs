using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Core.Contents.Internal;
using Lxna.Messages;
using Lxna.Rpc.Primitives;
using Omnix.Base;
using Omnix.Configuration;

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

        public override ServiceStateType StateType { get; }

        public IEnumerable<LxnaThumbnail> GetThumnails(string path, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            return _thumbnailCacheStorage.GetThumnailImages(path, width, height, formatType, resizeType, token);
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
               
            }
        }
    }
}
