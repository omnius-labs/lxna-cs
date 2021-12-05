using Omnius.Core;

namespace Omnius.Lxna.Components.Models;

public record FileSystemOptions
{
    public FileSystemOptions(IArchiveFileExtractorProvider archiveFileExtractorProvider, string temporaryDirectoryPath, IBytesPool bytesPool)
    {
        this.ArchiveFileExtractorProvider = archiveFileExtractorProvider;
        this.TemporaryDirectoryPath = temporaryDirectoryPath;
        this.BytesPool = bytesPool;
    }

    public IArchiveFileExtractorProvider ArchiveFileExtractorProvider { get; }
    public string TemporaryDirectoryPath { get; }
    public IBytesPool BytesPool { get; }
}
