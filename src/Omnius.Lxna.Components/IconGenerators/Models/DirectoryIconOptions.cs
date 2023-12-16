namespace Omnius.Lxna.Components.IconGenerators.Models;

public record DirectoryIconOptions
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required IconFormatType FormatType { get; init; }
}
