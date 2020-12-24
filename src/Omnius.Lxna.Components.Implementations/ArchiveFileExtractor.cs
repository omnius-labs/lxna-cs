
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Io;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;
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

        private static string NormalizeDirectoryPath(string path)
        {
            return path.TrimEnd('/', '\\') + "/";
        }

        public async ValueTask<IEnumerable<string>> FindDirectoriesAsync(string archiveFilePath, string? path, CancellationToken cancellationToken = default)
        {
            path = NormalizeDirectoryPath(path ?? "");

            var results = new List<string>();

            using var archiveFile = new ArchiveFile(archiveFilePath);
            foreach (var entry in archiveFile.Entries)
            {
                var fileName = entry.FileName;

                if (!entry.IsFolder)
                {
                    continue;
                }

                string relativePath = "";

                if (path is not null)
                {
                    if (fileName == path || !fileName.StartsWith(path))
                    {
                        continue;
                    }

                    relativePath = fileName.Remove(0, path.Length);
                }

                if (relativePath.Contains('/'))
                {
                    continue;
                }

                results.Add(path + relativePath);
            }

            return results;
        }

        public async ValueTask<IEnumerable<string>> FindFilesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            path = NormalizeDirectoryPath(path ?? "");

            var results = new List<string>();

            using var archiveFile = new ArchiveFile(archiveFilePath);
            foreach (var entry in archiveFile.Entries)
            {
                var fileName = entry.FileName;

                if (entry.IsFolder)
                {
                    continue;
                }

                string relativePath = "";

                if (path is not null)
                {
                    if (!fileName.StartsWith(path))
                    {
                        continue;
                    }

                    relativePath = fileName.Remove(0, path.Length);
                }

                if (relativePath.Contains('/'))
                {
                    continue;
                }

                results.Add(path + relativePath);
            }

            return results;
        }

        public async ValueTask<IMemoryOwner<byte>> ReadFileBytesAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries.FirstOrDefault(n => n.FileName == path);
            var memoryStream = new RecyclableMemoryStream(_bytesPool);
            entry.Extract(memoryStream);
            return memoryStream.ToMemoryOwner();
        }

        public async ValueTask<long> GetFileSizeAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries.FirstOrDefault(n => n.FileName == path);
            return (long)entry.Size;
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(string archiveFilePath, string path, CancellationToken cancellationToken = default)
        {
            using var archiveFile = new ArchiveFile(archiveFilePath);
            var entry = archiveFile.Entries.FirstOrDefault(n => n.FileName == path);
            var tempFileStream = await FileHelper.GenTempFileStreamAsync(_tempPath, _random, cancellationToken);
            entry.Extract(tempFileStream);
            return new ExtractedFileOwner(tempFileStream.Name);
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(IEnumerable<string> pathList, CancellationToken cancellationToken = default)
        {
            var values = pathList.ToList();

            if (values.Count == 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(pathList)} is empty");
            }

            if (values.Count == 1)
            {
                return new PhysicalFileOwner(values[0]);
            }

            var extractedFileOwners = new List<IFileOwner>();

            try
            {
                while (values.Count >= 2)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileOwner = await this.ExtractFileAsync(values[0], values[1], cancellationToken);
                    extractedFileOwners.Add(fileOwner);
                    values.RemoveAt(0);
                    values[0] = fileOwner.Path;
                }

                return extractedFileOwners[^1];
            }
            finally
            {
                foreach (var fileOwner in extractedFileOwners.ToArray()[..^1])
                {
                    fileOwner.Dispose();
                }
            }
        }

        private class PhysicalFileOwner : IFileOwner
        {
            public PhysicalFileOwner(string path)
            {
                this.Path = path;
            }

            public string Path { get; }

            public void Dispose()
            {
            }
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
