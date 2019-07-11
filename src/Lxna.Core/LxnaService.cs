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
using Omnix.Network;

namespace Lxna.Core
{
    public sealed class LxnaService : ServiceBase, ILxnaService
    {
        private readonly LxnaOptions _options;
        private readonly ContentExplorer _contentExplorer;

        private ServiceStateType _state = ServiceStateType.Stopped;

        private volatile bool _disposed;

        public LxnaService(LxnaOptions options)
        {
            _options = options;
            _contentExplorer = new ContentExplorer(_options);
        }

        public IEnumerable<LxnaContentId> GetContentIds(OmniAddress? address, CancellationToken token = default)
        {
            return _contentExplorer.GetContentIds(address, token);
        }

        public IEnumerable<LxnaThumbnail> GetThumbnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            return _contentExplorer.GetThumnails(address, width, height, formatType, resizeType, token);
        }

        public void ReadContent(OmniAddress address, long position, Span<byte> buffer, CancellationToken token = default)
        {
            _contentExplorer.ReadContent(address, position, buffer, token);
        }

        protected override async ValueTask OnStart()
        {
            this.StateType = ServiceStateType.Starting;
            this.StateType = ServiceStateType.Running;
        }

        protected override async ValueTask OnStop()
        {
            this.StateType = ServiceStateType.Stopping;
            this.StateType = ServiceStateType.Stopped;
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
                _contentExplorer.Dispose();
            }
        }
    }
}
