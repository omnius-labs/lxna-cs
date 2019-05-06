using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Configuration;

namespace Lxna.Rpc
{
    public interface ILxnaService : IService, ISettings
    {
        IEnumerable<ContentId> GetContentIds(string? path, CancellationToken token = default);
        IEnumerable<Thumbnail> GetThumbnails(string path, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken token = default);
        void ReadContent(string path, long position, Span<byte> buffer, CancellationToken token = default);
    }
}
