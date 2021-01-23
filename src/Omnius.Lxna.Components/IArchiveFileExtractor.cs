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
        public string? ArchiveFilePath { get; init; }

        public string? TemporaryDirectoryPath { get; init; }

        public IBytesPool? BytesPool { get; init; }
    }

    public interface IArchiveFileExtractor : IDisposable
    {
        ValueTask<bool> ExistsFileAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<DateTime> GetFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<string>> FindDirectoriesAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<(IEnumerable<string>, IEnumerable<string>)> FindDirectoriesAndArchiveFilesAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<IEnumerable<string>> FindFilesAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<Stream> GetFileStreamAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default);

        ValueTask<IFileOwner> ExtractFileAsync(string path, CancellationToken cancellationToken = default);
    }
}
