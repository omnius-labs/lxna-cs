using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Omnius.Lxna.Launcher.Helpers;

namespace Omnius.Lxna.Launcher;

public static class Runner
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static async ValueTask RunAsync()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            var fileLock = new FileLock(Path.Combine(basePath, "lock"));

            using (fileLock.Lock(TimeSpan.FromSeconds(30)))
            {
                // gen bin path
                var uiDesktopPath = Path.Combine(basePath, "bin/ui-desktop/Omnius.Lxna.Ui.Desktop");

                // add ext to suffix
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    uiDesktopPath += ".exe";
                }

                // gen storage path
                var uiDesktopStoragePath = Path.Combine(basePath, "storage/ui-desktop");

                // find free port
                var listenPort = FindFreeTcpPort();

                // start ui-desktop
                var uiDesktopProcessInfo = new ProcessStartInfo()
                {
                    FileName = uiDesktopPath,
                    WorkingDirectory = Path.GetDirectoryName(uiDesktopPath),
                    Arguments = $"-s {uiDesktopStoragePath}",
                    UseShellExecute = false,
                };
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

    private static int FindFreeTcpPort()
    {
        for (int i = 0; ; i++)
        {
            try
            {
                var port = Random.Shared.Next(10000, 60000);
                using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                socket.Close();

                return port;
            }
            catch (Exception)
            {
                if (i >= 10) throw;
            }
        }
    }
}
