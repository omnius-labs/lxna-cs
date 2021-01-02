using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;

namespace Omnius.Lxna.Components
{
    public interface IArchiveFileExtractorFactory
    {
        ValueTask<IArchiveFileExtractor> CreateAsync(ArchiveFileExtractorOptions options);
    }

    public class ArchiveFileExtractorOptions
    {
        public string? TemporaryDirectoryPath { get; init; }

        public IBytesPool? BytesPool { get; init; }
    }

    public interface IArchiveFileExtractor : IAsyncDisposable
    {
        ValueTask<bool> ExistsFileAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsDirectoryAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<DateTime> GetFileLastWriteTimeAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<string>> FindDirectoriesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<string>> FindFilesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<Stream> GetPhysicalFileStreamAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<long> GetFileSizeAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);

        ValueTask<IFileOwner> ExtractFileAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default);
    }
}
