namespace Omnius.Lxna.Components;

public sealed partial class ArchiveFileExtractorProvider
{
    private class ExtractorEntry
    {
        public ExtractorEntry(IArchiveFileExtractor archiveFileExtractor, string filePath, DateTime lastAccessTime)
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
