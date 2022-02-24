using CommandLine;

namespace Omnius.Lxna.Ui.Desktop;

public class Options
{
    [Option('s', "storage")]
    public string StorageDirectoryPath { get; set; } = "../storage";

    [Option('v', "verbose")]
    public bool Verbose { get; set; } = false;
}
