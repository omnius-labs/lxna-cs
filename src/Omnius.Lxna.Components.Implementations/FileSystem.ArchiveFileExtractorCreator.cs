using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public sealed partial class FileSystem
    {
        internal sealed class ArchiveFileExtractorCreator : DisposableBase
        {
            private readonly IArchiveFileExtractorFactory _archiveFileExtractorFactory;
            private readonly string _tempDirPath;
            private readonly IBytesPool _bytesPool;

            private readonly Dictionary<NestedPath, ArchiveFileExtractorEntry> _entries = new();

            private readonly AsyncLock _asyncLock = new();
            private const int MaxCacheCount = 256;

            public ArchiveFileExtractorCreator(IArchiveFileExtractorFactory archiveFileExtractorFactory, string tempDirPath, IBytesPool bytesPool)
            {
                _archiveFileExtractorFactory = archiveFileExtractorFactory;
                _tempDirPath = tempDirPath;
                _bytesPool = bytesPool;
            }

            protected override void OnDispose(bool disposing)
            {
                foreach (var entry in _entries)
                {
                    entry.Value.ArchiveFileExtractor.Dispose();
                }

                _entries.Clear();
            }

            public async ValueTask<IArchiveFileExtractor> GetArchiveFileExtractorAsync(NestedPath path, CancellationToken cancellationToken = default)
            {
                if (path.Values.Count == 0) throw new ArgumentOutOfRangeException($"{nameof(path.Values)} is empty");

                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    if (_entries.TryGetValue(path, out var entry))
                    {
                        entry.LastAccessTime = DateTime.UtcNow;
                        return entry.ArchiveFileExtractor;
                    }

                    string lastArchiveFilePath;
                    IArchiveFileExtractor lastArchiveFileExtractor = null!;

                    for (int i = 1; i <= path.Values.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var targetPath = new NestedPath(path.Values[0..i]);

                        if (_entries.TryGetValue(targetPath, out entry))
                        {
                            lastArchiveFilePath = entry.FilePath;
                            lastArchiveFileExtractor = entry.ArchiveFileExtractor;

                            continue;
                        }

                        if (targetPath.Values.Count == 1)
                        {
                            lastArchiveFilePath = targetPath.Values[0];
                        }
                        else
                        {
                            using var tempFileStream = TempFileHelper.GenStream(_tempDirPath, targetPath.GetExtension());
                            await lastArchiveFileExtractor.ExtractFileAsync(targetPath.Values[^1], tempFileStream, cancellationToken);
                            lastArchiveFilePath = tempFileStream.Name;
                        }

                        var archiveFileExtractorOptions = new ArchiveFileExtractorOptions()
                        {
                            ArchiveFilePath = lastArchiveFilePath,
                            BytesPool = _bytesPool,
                        };
                        var archiveFileExtractor = await _archiveFileExtractorFactory.CreateAsync(archiveFileExtractorOptions);

                        _entries.Add(targetPath, new ArchiveFileExtractorEntry(archiveFileExtractor, lastArchiveFilePath, DateTime.UtcNow));
                        lastArchiveFileExtractor = archiveFileExtractor;
                    }

                    return lastArchiveFileExtractor;
                }
            }

            public async ValueTask ShrinkAsync(CancellationToken cancellationToken = default)
            {
                using (await _asyncLock.LockAsync(cancellationToken))
                {
                    int removeCount = _entries.Count - MaxCacheCount;
                    if (removeCount <= 0) return;

                    foreach (var key in _entries.OrderBy(n => n.Value.LastAccessTime).Take(removeCount).Select(n => n.Key).ToArray())
                    {
                        _entries.Remove(key);
                    }
                }
            }

            private class ArchiveFileExtractorEntry
            {
                public ArchiveFileExtractorEntry(IArchiveFileExtractor archiveFileExtractor, string filePath, DateTime lastAccessTime)
                {
                    this.ArchiveFileExtractor = archiveFileExtractor;
                    this.FilePath = filePath;
                    this.LastAccessTime = lastAccessTime;
                }

                public IArchiveFileExtractor ArchiveFileExtractor { get; }

                public string FilePath { get; }

                public DateTime LastAccessTime { get; set; }
            }
        }
    }
}
