namespace Omnius.Lxna.Components.IconGenerators.Models;

public readonly struct DirectoryIconResult
{
    public DirectoryIconResult(DirectoryIconResultStatus status, IconContent? content = null)
    {
        this.Status = status;
        this.Content = content;
    }

    public DirectoryIconResultStatus Status { get; }
    public IconContent? Content { get; }
}

public enum DirectoryIconResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
