using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Collections;
using Omnius.Core.Network;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public class LxnaFileSystemOptions
    {
        public IArchiveFileExtractor? ArchiveFileExtractor { get; init; }

        public IBytesPool? BytesPool { get; init; }
    }

    public interface ILxnaFileSystemFactory
    {
        ValueTask<ILxnaFileSystem> CreateAsync(LxnaFileSystemOptions options);
    }

    public interface ILxnaFileSystem : IAsyncDisposable
    {
        ValueTask<IEnumerable<LxnaPath>> FindDirectoriesAsync(LxnaPath? path = null, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<LxnaPath>> FindFilesAsync(LxnaPath? path = null, CancellationToken cancellationToken = default);

        ValueTask<IMemoryOwner<byte>> ReadFileBytesAsync(LxnaPath path, CancellationToken cancellationToken = default);

        ValueTask<long> GetFileSizeAsync(LxnaPath path, CancellationToken cancellationToken = default);

        ValueTask<IFileOwner> ExtractFileAsync(LxnaPath path, CancellationToken cancellationToken = default);
    }
}
