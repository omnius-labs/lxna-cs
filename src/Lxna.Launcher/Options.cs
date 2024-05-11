using CommandLine;

namespace Lxna.Launcher;

public class Options
{
    [Option('m', "mode")]
    public string? Mode { get; set; } = null;
}
