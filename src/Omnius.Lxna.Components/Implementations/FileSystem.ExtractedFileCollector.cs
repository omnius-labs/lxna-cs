using System.Buffers;
using Omnius.Core;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components;

public sealed partial class FileSystem
{
    internal sealed class ExtractedFileCollector : DisposableBase
    {
        private readonly IArchiveFileExtractorProvider _archiveFileExtractorProvider;
        private readonly string _tempDirPath;

        private readonly Dictionary<NestedPath, ExtractedFileEntry> _entries = new();
        private readonly AsyncLock _asyncLock = new();

        public ExtractedFileCollector(IArchiveFileExtractorProvider archiveFileExtractorProvider, string tempDirPath)
        {
            _archiveFileExtractorProvider = archiveFileExtractorProvider;
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

        public async ValueTask<IExtractedFileOwner> GetExtractedFileAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            using (await _asyncLock.LockAsync(cancellationToken))
            {
                if (!_entries.TryGetValue(path, out var entry))
                {
                    var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                    var archiveFileExtractor = await _archiveFileExtractorProvider.CreateAsync(archiveFilePath, cancellationToken);

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
                await _archiveFileExtractorProvider.ShrinkAsync(cancellationToken);

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

        private class ExtractedFileOwner : IExtractedFileOwner
        {
            private readonly ExtractedFileEntry _entry;
            private readonly AsyncLock _asyncLock;

            public ExtractedFileOwner(ExtractedFileEntry entry, AsyncLock asyncLock)
            {
                _entry = entry;
                _asyncLock = asyncLock;

                this.Path = _entry.FilePath;
            }

            public void Dispose()
            {
                using (_asyncLock.Lock())
                {
                    _entry.ReferenceCount--;
                }
            }

            public string Path { get; }
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
}
