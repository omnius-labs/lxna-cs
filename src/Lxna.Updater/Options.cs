using CommandLine;

namespace Lxna.Launcher;

public class Options
{
    [Option('b', "base")]
    public string? BasePath { get; set; } = null;
}
