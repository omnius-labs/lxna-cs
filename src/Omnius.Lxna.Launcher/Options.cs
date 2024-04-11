using CommandLine;

namespace Omnius.Lxna.Launcher;

public class Options
{
    [Option('m', "mode")]
    public string? Mode { get; set; } = null;
}
