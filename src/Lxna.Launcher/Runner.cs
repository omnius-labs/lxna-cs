using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Lxna.Launcher.Helpers;

namespace Lxna.Launcher;

public static class Runner
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static async ValueTask RunAsync()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            var fileLock = new FileLock(Path.Combine(basePath, "lock"));

            using (await fileLock.LockAsync(TimeSpan.FromSeconds(30)))
            {
                // gen bin path
                var uiDesktopPath = Path.Combine(basePath, "bin/ui-desktop/Lxna.Ui.Desktop");

                // add ext to suffix
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    uiDesktopPath += ".exe";
                }

                // gen storage path
                var uiDesktopStoragePath = Path.Combine(basePath, "storage/ui-desktop");

                // start ui-desktop
                var uiDesktopProcessInfo = new ProcessStartInfo()
                {
                    FileName = uiDesktopPath,
                    WorkingDirectory = Path.GetDirectoryName(uiDesktopPath),
                    Arguments = $"-s {uiDesktopStoragePath}",
                    UseShellExecute = false,
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    uiDesktopProcessInfo.Environment["LANG"] = "en_US.UTF-8";
                    uiDesktopProcessInfo.Environment["AVALONIA_SCREEN_SCALE_FACTORS"] = "XWAYLAND0=2";
                }

                using var uiDesktopProcess = Process.Start(uiDesktopProcessInfo);

                // wait for ui-desktop exit
                await uiDesktopProcess!.WaitForExitAsync();
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unexpected Exception");
        }
    }
}
