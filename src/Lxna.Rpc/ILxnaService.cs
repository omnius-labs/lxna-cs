using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Configuration;
using Lxna.Rpc.Primitives;
using Omnix.Network;

namespace Lxna.Rpc
{
    public interface ILxnaService : IService, ISettings
    {
        IEnumerable<LxnaContentId> GetContentIds(OmniAddress? address, CancellationToken token = default);
        IEnumerable<LxnaThumbnail> GetThumbnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default);
        void ReadContent(OmniAddress address, long position, Span<byte> buffer, CancellationToken token = default);
    }
}
