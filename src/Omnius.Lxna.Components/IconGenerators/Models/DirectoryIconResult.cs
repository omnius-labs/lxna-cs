namespace Omnius.Lxna.Components.IconGenerators.Models;

public record struct DirectoryIconResult
{
    public DirectoryIconResultStatus Status { get; init; }
    public IconContent? Content { get; set; }
}

public enum DirectoryIconResultStatus
{
    Unknown,
    Succeeded,
    Failed,
}
