using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public sealed partial class FileSystem : AsyncDisposableBase, IFileSystem
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly HashSet<string> _archiveFileExtensionList = new() { ".zip", ".rar", ".7z" };

        private readonly IArchiveFileExtractorFactory _archiveFileExtractorFactory;
        private readonly string _tempDirPath;
        private readonly IBytesPool _bytesPool;

        private readonly ArchiveFileExtractorCreator _archiveFileExtractorCreator;
        private readonly ExtractedFileCollector _extractedFileCollector;

        private Task _watchTask = null!;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        private readonly AsyncReaderWriterLock _asyncLock = new();

        internal sealed class FileSystemFactory : IFileSystemFactory
        {
            public async ValueTask<IFileSystem> CreateAsync(FileSystemOptions options)
            {
                var result = new FileSystem(options);
                await result.InitAsync();

                return result;
            }
        }

        public static IFileSystemFactory Factory { get; } = new FileSystemFactory();

        internal FileSystem(FileSystemOptions options)
        {
            _archiveFileExtractorFactory = options.ArchiveFileExtractorFactory ?? throw new ArgumentException($"{nameof(options.ArchiveFileExtractorFactory)} is null");
            _tempDirPath = options.TemporaryDirectoryPath ?? Path.Combine(Path.GetTempPath(), "FileSystem");
            _bytesPool = options.BytesPool ?? BytesPool.Shared;

            _archiveFileExtractorCreator = new ArchiveFileExtractorCreator(_archiveFileExtractorFactory, _tempDirPath, _bytesPool);
            _extractedFileCollector = new ExtractedFileCollector(_archiveFileExtractorCreator, _tempDirPath);
        }

        internal async ValueTask InitAsync()
        {
            _watchTask = this.WatchAsync(_cancellationTokenSource.Token);
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            await _watchTask;
            _cancellationTokenSource.Dispose();

            _extractedFileCollector.Dispose();
            _archiveFileExtractorCreator.Dispose();
        }

        private async Task WatchAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                for (; ; )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                    using (await _asyncLock.WriterLockAsync(cancellationToken))
                    {
                        await _archiveFileExtractorCreator.ShrinkAsync(cancellationToken);
                        await _extractedFileCollector.ShrinkAsync(cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                _logger.Info(e);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public async ValueTask<bool> ExistsFileAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    var result = await this.ExistsPhysicalFileAsync(path.Values[0], cancellationToken);
                    return result;
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.ExistsFileAsync(path.Values[^1], cancellationToken);
                    return result;
                }
            }
        }

        private async ValueTask<bool> ExistsPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
        {
            return File.Exists(path);
        }

        public async ValueTask<bool> ExistsDirectoryAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    var result = await this.ExistsPhysicalDirectoryAsync(path.Values[0], cancellationToken);
                    return result;
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.ExistsDirectoryAsync(path.Values[^1], cancellationToken);
                    return result;
                }
            }
        }

        private async ValueTask<bool> ExistsPhysicalDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            return Directory.Exists(path);
        }

        public async ValueTask<DateTime> GetFileLastWriteTimeAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    var result = await this.GetPhysicalFileLastWriteTimeAsync(path.Values[0], cancellationToken);
                    return result;
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.GetFileLastWriteTimeAsync(path.Values[^1], cancellationToken);
                    return result;
                }
            }
        }

        private async ValueTask<DateTime> GetPhysicalFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LastAccessTimeUtc;
        }

        public async ValueTask<IEnumerable<NestedPath>> FindDirectoriesAsync(NestedPath? path = null, CancellationToken cancellationToken = default)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path is null || path.Values.Count == 0)
                {
                    var result = await this.FindRootPhysicalDirectoriesAsync(cancellationToken);
                    var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                    list.Sort();
                    return list;
                }
                else if (path.Values.Count == 1)
                {
                    var result = await this.FindPhysicalDirectoriesAsync(path.Values[0], cancellationToken);
                    var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                    list.Sort();
                    return list;
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.FindDirectoriesAsync(path.Values[^1], cancellationToken);
                    var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).ToList();
                    list.Sort();
                    return list;
                }
            }
        }

        private async ValueTask<IEnumerable<string>> FindRootPhysicalDirectoriesAsync(CancellationToken cancellationToken = default)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Directory.GetLogicalDrives().ToArray();
            }
            else
            {
                return new string[] { "/" };
            }
        }

        private async ValueTask<IEnumerable<string>> FindPhysicalDirectoriesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetDirectories(path).Select(n => PathHelper.Normalize(n)).ToList();
        }

        public async ValueTask<IEnumerable<NestedPath>> FindArchiveFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    var result = await this.FindPhysicalArchiveFilesAsync(path.Values[0], cancellationToken);
                    var list = result.Select(n => new NestedPath(new[] { n, "" })).ToList();
                    list.Sort();
                    return list;
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.FindArchiveFilesAsync(path.Values[^1], cancellationToken);
                    var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).Append("").ToArray())).ToList();
                    list.Sort();
                    return list;
                }
            }
        }

        private async ValueTask<IEnumerable<string>> FindPhysicalArchiveFilesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(path).Select(n => PathHelper.Normalize(n)).Where(n => _archiveFileExtensionList.Contains(Path.GetExtension(n))).ToList();
        }

        public async ValueTask<IEnumerable<NestedPath>> FindFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    var result = await this.FindPhysicalFileAsync(path.Values[0], cancellationToken);
                    var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                    list.Sort();
                    return list;
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.FindFilesAsync(path.Values[^1], cancellationToken);
                    var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).ToList();
                    list.Sort();
                    return list;
                }
            }
        }

        private async ValueTask<IEnumerable<string>> FindPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(path).Select(n => PathHelper.Normalize(n)).ToList();
        }

        public async ValueTask<Stream> GetFileStreamAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    return await this.GetPhysicalFileStreamAsync(path.Values[0], cancellationToken);
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.GetFileStreamAsync(path.Values[^1], cancellationToken);
                    return result;
                }
            }
        }

        private async ValueTask<Stream> GetPhysicalFileStreamAsync(string path, CancellationToken cancellationToken = default)
        {
            var fileStream = new FileStream(path, FileMode.Open);
            return fileStream;
        }

        public async ValueTask<long> GetFileSizeAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    return await this.GetPhysicalFileSizeAsync(path.Values[0], cancellationToken);
                }
                else
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                    var result = await archiveFileExtractor.GetFileSizeAsync(path.Values[^1], cancellationToken);
                    return result;
                }
            }
        }

        private async ValueTask<long> GetPhysicalFileSizeAsync(string path, CancellationToken cancellationToken = default)
        {
            return new FileInfo(path).Length;
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0) throw new ArgumentException($"{nameof(path)} is invalid");

            await Task.Delay(1, cancellationToken).ConfigureAwait(false);

            using (await _asyncLock.ReaderLockAsync(cancellationToken))
            {
                if (path.Values.Count == 1)
                {
                    return new PhysicalFileOwner(path.Values[0]);
                }
                else
                {
                    return await _extractedFileCollector.GetFileOwnerAsync(path, cancellationToken);
                }
            }
        }

        internal sealed class ExtractedFileCollector : DisposableBase
        {
            private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

            private readonly ArchiveFileExtractorCreator _archiveFileExtractorCreator;
            private readonly string _tempDirPath;

            private readonly Dictionary<NestedPath, ExtractedFileEntry> _entries = new();
            private readonly AsyncLock _asyncLock = new();

            public ExtractedFileCollector(ArchiveFileExtractorCreator archiveFileExtractorCreator, string tempDirPath)
            {
                _archiveFileExtractorCreator = archiveFileExtractorCreator;
                _tempDirPath = tempDirPath;
            }

            protected override void OnDispose(bool disposing)
            {
                foreach (var entry in _entries)
                {
                    try
                    {
                        File.Delete(entry.Value.FilePath);
                    }
                    catch (Exception e)
                    {
                        _logger.Debug(e);
                    }
                }

                _entries.Clear();
            }

            public async ValueTask<IFileOwner> GetFileOwnerAsync(NestedPath path, CancellationToken cancellationToken = default)
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    if (!_entries.TryGetValue(path, out var entry))
                    {
                        var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                        var archiveFileExtractor = await _archiveFileExtractorCreator.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);

                        using var tempFileStream = TempFileHelper.GenStream(_tempDirPath, path.GetExtension());
                        await archiveFileExtractor.ExtractFileAsync(path.Values[^1], tempFileStream, cancellationToken);

                        entry = new ExtractedFileEntry(tempFileStream.Name);
                        _entries[path] = entry;
                    }

                    entry.ReferenceCount++;
                    return new ExtractedFileOwner(entry, _asyncLock);
                }
            }

            public async ValueTask ShrinkAsync(CancellationToken cancellationToken = default)
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    foreach (var (key, value) in _entries.Where(n => n.Value.ReferenceCount == 0).ToArray())
                    {
                        try
                        {
                            File.Delete(value.FilePath);
                            _entries.Remove(key);
                        }
                        catch (Exception e)
                        {
                            _logger.Debug(e);
                        }
                    }
                }
            }

            private class ExtractedFileOwner : IFileOwner
            {
                private readonly ExtractedFileEntry _entry;
                private readonly AsyncLock _asyncLock;

                public ExtractedFileOwner(ExtractedFileEntry entry, AsyncLock asyncLock)
                {
                    _entry = entry;
                    _asyncLock = asyncLock;

                    this.Path = _entry.FilePath;
                }

                public string Path { get; }

                public void Dispose()
                {
                    using (_asyncLock.Lock())
                    {
                        _entry.ReferenceCount--;
                    }
                }
            }

            private sealed class ExtractedFileEntry
            {
                public ExtractedFileEntry(string filePath)
                {
                    this.FilePath = filePath;
                }

                public string FilePath { get; }

                public int ReferenceCount { get; set; }
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
    }
}
