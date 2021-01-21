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
    public sealed class ArchiveFileExtractor : DisposableBase, IArchiveFileExtractor
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _archiveFilePath;
        private readonly string _tempDirPath;
        private readonly IBytesPool _bytesPool;

        private ArchiveFile _archiveFile = null!;
        private Dictionary<string, Entry> _entryMap = new();

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
            _archiveFilePath = options.ArchiveFilePath ?? throw new ArgumentNullException(options.ArchiveFilePath);
            _tempDirPath = options.TemporaryDirectoryPath ?? Path.Combine(Path.GetTempPath(), "ArchiveFileExtractor");
            _bytesPool = options.BytesPool ?? BytesPool.Shared;
        }

        internal async ValueTask InitAsync()
        {
            await Task.Delay(1).ConfigureAwait(false);

            _archiveFile = new ArchiveFile(_archiveFilePath);

            foreach (var entry in _archiveFile.Entries)
            {
                _entryMap.Add(PathHelper.Normalize(entry.FileName), entry);
            }
        }

        protected override void OnDispose(bool disposing)
        {
            _archiveFile.Dispose();
            _entryMap.Clear();
        }

        public async ValueTask<bool> ExistsFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_entryMap.TryGetValue(path, out var entry) && !entry.IsFolder)
            {
                return true;
            }

            return false;
        }

        public async ValueTask<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_entryMap.TryGetValue(path, out var entry) && entry.IsFolder)
            {
                return true;
            }

            return false;
        }

        public async ValueTask<DateTime> GetFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_entryMap.TryGetValue(path, out var entry) && !entry.IsFolder)
            {
                return entry.LastWriteTime.ToUniversalTime();
            }

            throw new FileNotFoundException();
        }

        public async ValueTask<IEnumerable<string>> FindDirectoriesAsync(string path, CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            foreach (var (filePath, entry) in _entryMap)
            {
                if (!entry.IsFolder)
                {
                    continue;
                }

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

        public async ValueTask<IEnumerable<string>> FindFilesAsync(string path, CancellationToken cancellationToken = default)
        {
            var results = new List<string>();

            foreach (var (filePath, entry) in _entryMap)
            {
                if (entry.IsFolder)
                {
                    continue;
                }

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

        public async ValueTask<Stream> GetFileStreamAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_entryMap.TryGetValue(path, out var entry) && !entry.IsFolder)
            {
                var memoryStream = new RecyclableMemoryStream(_bytesPool);
                entry.Extract(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }

            throw new FileNotFoundException();
        }

        public async ValueTask<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_entryMap.TryGetValue(path, out var entry) && !entry.IsFolder)
            {
                return (long)entry.Size;
            }

            throw new FileNotFoundException();
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_entryMap.TryGetValue(path, out var entry) && !entry.IsFolder)
            {
                var tempFileStream = await FileHelper.GenTempFileStreamAsync(_tempDirPath, Path.GetExtension(path), _random, cancellationToken);
                entry.Extract(tempFileStream);
                return new ExtractedFileOwner(tempFileStream.Name);
            }

            throw new FileNotFoundException();
        }

        private class ExtractedFileOwner : AsyncDisposableBase, IFileOwner
        {
            public ExtractedFileOwner(string path)
            {
                this.Path = path;
            }

            public string Path { get; }

            protected override async ValueTask OnDisposeAsync()
            {
                await Task.Delay(1).ConfigureAwait(false);

                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(this.Path);
                        return;
                    }
                    catch (IOException)
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }
    }
}
