using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Io;
using Omnius.Lxna.Components.Internal.Helpers;
using SevenZipExtractor;

namespace Omnius.Lxna.Components
{
    public sealed class ArchiveFileExtractor : AsyncDisposableBase, IArchiveFileExtractor
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _tempPath;
        private readonly IBytesPool _bytesPool;

        private readonly Random _random = new Random();

        internal sealed class ArchiveFileExtractorFactory : IArchiveFileExtractorFactory
        {
            public async ValueTask<IArchiveFileExtractor> CreateAsync(ArchiveFileExtractorOptions options)
            {
                var result = new ArchiveFileExtractor(options);
                await result.InitAsync();

                return result;
            }
        }

        public static IArchiveFileExtractorFactory Factory { get; } = new ArchiveFileExtractorFactory();

        internal ArchiveFileExtractor(ArchiveFileExtractorOptions options)
        {
            _tempPath = options.TemporaryDirectoryPath ?? Path.Combine(Path.GetTempPath(), "ArchiveFileExtractor");
            _bytesPool = options.BytesPool ?? BytesPool.Shared;
        }

        internal async ValueTask InitAsync()
        {
        }

        protected override async ValueTask OnDisposeAsync()
        {
        }

        public async ValueTask<bool> ExistsFileAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            return archiveFile.Entries
                .Where(n => !n.IsFolder)
                .Select(n => PathHelper.Normalize(n.FileName))
                .Any(n => n == path);
        }

        public async ValueTask<bool> ExistsDirectoryAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            return archiveFile.Entries
                .Where(n => n.IsFolder)
                .Select(n => PathHelper.Normalize(n.FileName))
                .Any(n => n == path);
        }

        public async ValueTask<DateTime> GetFileLastWriteTimeAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries
                .Where(n => !n.IsFolder)
                .FirstOrDefault(n => PathHelper.Normalize(n.FileName) == path);

            if (entry is null)
            {
                throw new FileNotFoundException();
            }

            return entry.LastWriteTime.ToUniversalTime();
        }

        public async ValueTask<IEnumerable<string>> FindDirectoriesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            using var archiveFile = new ArchiveFile(archiveFilePath);
            foreach (var entry in archiveFile.Entries)
            {
                if (!entry.IsFolder)
                {
                    continue;
                }

                var filePath = PathHelper.Normalize(entry.FileName);
                if (filePath == path || !filePath.StartsWith(path))
                {
                    continue;
                }

                string relativePath = filePath.Remove(0, path.Length).TrimStart('/');
                if (relativePath.Contains('/'))
                {
                    continue;
                }

                results.Add(filePath);
            }

            return results;
        }

        public async ValueTask<IEnumerable<string>> FindFilesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            using var archiveFile = new ArchiveFile(archiveFilePath);
            foreach (var entry in archiveFile.Entries)
            {
                if (entry.IsFolder)
                {
                    continue;
                }

                var filePath = PathHelper.Normalize(entry.FileName);
                if (filePath == path || !filePath.StartsWith(path))
                {
                    continue;
                }

                string relativePath = filePath.Remove(0, path.Length).TrimStart('/');
                if (relativePath.Contains('/'))
                {
                    continue;
                }

                results.Add(filePath);
            }

            return results;
        }

        public async ValueTask<Stream> GetPhysicalFileStreamAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries
                .Where(n => !n.IsFolder)
                .FirstOrDefault(n => PathHelper.Normalize(n.FileName) == path);

            var memoryStream = new RecyclableMemoryStream(_bytesPool);
            entry.Extract(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        public async ValueTask<long> GetFileSizeAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries
                .Where(n => !n.IsFolder)
                .FirstOrDefault(n => PathHelper.Normalize(n.FileName) == path);

            return (long)entry.Size;
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries
                .Where(n => !n.IsFolder)
                .FirstOrDefault(n => PathHelper.Normalize(n.FileName) == path);

            var tempFileStream = await FileHelper.GenTempFileStreamAsync(_tempPath, _random, cancellationToken);
            entry.Extract(tempFileStream);
            return new ExtractedFileOwner(tempFileStream.Name);
        }

        private class ExtractedFileOwner : IFileOwner
        {
            public ExtractedFileOwner(string path)
            {
                this.Path = path;
            }

            public string Path { get; }

            public void Dispose()
            {
            }
        }
    }
}
