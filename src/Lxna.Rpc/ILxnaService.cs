using System;
using System.Collections.Generic;
using System.Threading;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Network;

namespace Lxna.Rpc
{
    public interface ILxnaService : IService
    {
        IEnumerable<LxnaContentId> GetContentIds(OmniAddress? address, CancellationToken token = default);
        IEnumerable<LxnaThumbnail> GetThumbnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default);
        int FileRead(OmniAddress address, long position, Span<byte> buffer, CancellationToken token = default);
    }
}
