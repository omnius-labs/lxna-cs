using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public interface IFileSystemFactory
    {
        ValueTask<IFileSystem> CreateAsync(FileSystemOptions options);
    }

    public class FileSystemOptions
    {
        public IArchiveFileExtractor? ArchiveFileExtractor { get; init; }

        public IBytesPool? BytesPool { get; init; }
    }

    public interface IFileSystem : IAsyncDisposable
    {
        ValueTask<bool> ExistsFileAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsDirectoryAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<DateTime> GetFileLastWriteTimeAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<NestedPath>> FindDirectoriesAsync(NestedPath? path = null, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<NestedPath>> FindArchiveFilesAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<NestedPath>> FindFilesAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<Stream> GetFileStreamAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<long> GetFileSizeAsync(NestedPath path, CancellationToken cancellationToken = default);

        ValueTask<IFileOwner> ExtractFileAsync(NestedPath path, CancellationToken cancellationToken = default);
    }
}
