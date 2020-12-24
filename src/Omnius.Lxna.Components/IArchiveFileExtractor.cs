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
    public class ArchiveFileExtractorOptions
    {
        public string? TemporaryDirectoryPath { get; init; }

        public IBytesPool? BytesPool { get; init; }
    }

    public interface IArchiveFileExtractorFactory
    {
        ValueTask<IArchiveFileExtractor> CreateAsync(ArchiveFileExtractorOptions options);
    }

    public interface IArchiveFileExtractor : IAsyncDisposable
    {
        ValueTask<IEnumerable<string>> FindDirectoriesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<string>> FindFilesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IMemoryOwner<byte>> ReadFileBytesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<long> GetFileSizeAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IFileOwner> ExtractFileAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IFileOwner> ExtractFileAsync(IEnumerable<string> pathList, CancellationToken cancellationToken = default);
    }
}
