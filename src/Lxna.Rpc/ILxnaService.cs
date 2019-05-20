using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Configuration;
using Lxna.Rpc.Primitives;

namespace Lxna.Rpc
{
    public interface ILxnaService : IService, ISettings
    {
        IEnumerable<LxnaContentId> GetContentIds(string? path, CancellationToken token = default);
        IEnumerable<LxnaThumbnail> GetThumbnails(string path, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default);
        void ReadContent(string path, long position, Span<byte> buffer, CancellationToken token = default);
    }
}
