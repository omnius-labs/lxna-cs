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
    public interface ILxnaService: IService, ISettings
    {
        IEnumerable<FileMetadata> GetFileMetadatas(string path, CancellationToken token = default);
        IEnumerable<ThumbnailImage> GetFileThumbnail(string path, int width, int height, CancellationToken token = default);
        void ReadFileContent(string path, long position, Span<byte> buffer, CancellationToken token = default);
    }
}
