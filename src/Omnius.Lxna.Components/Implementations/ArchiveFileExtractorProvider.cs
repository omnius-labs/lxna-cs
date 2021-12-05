using Omnius.Core;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components;

public sealed partial class ArchiveFileExtractorProvider : DisposableBase, IArchiveFileExtractorProvider
{
    private readonly string _tempDirPath;
    private readonly IBytesPool _bytesPool;

    private readonly Dictionary<NestedPath, ExtractorEntry> _entries = new();

    private readonly AsyncLock _asyncLock = new();
    private const int MaxCacheCount = 256;

    private ArchiveFileExtractorProvider(string tempDirPath, IBytesPool bytesPool)
    {
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

    public async ValueTask<IArchiveFileExtractor> CreateAsync(NestedPath path, CancellationToken cancellationToken = default)
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

                var archiveFileExtractor = await Extractor.CreateAsync(lastArchiveFilePath, _bytesPool);

                _entries.Add(targetPath, new ExtractorEntry(archiveFileExtractor, lastArchiveFilePath, DateTime.UtcNow));
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
}
