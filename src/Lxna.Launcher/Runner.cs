using System.Collections.Immutable;
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

                var envPath = Path.Combine(basePath, ".env." + GetCurrentPlatform());
                foreach (var (key, value) in ParseEnvFile(envPath))
                {
                    uiDesktopProcessInfo.Environment[key] = value;
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

    public static string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    private static IReadOnlyDictionary<string, string> ParseEnvFile(string path)
    {
        using var reader = new StreamReader(path);
        var builder = ImmutableDictionary.CreateBuilder<string, string>();

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;
            builder[parts[0]] = parts[1];
        }

        return builder.ToImmutable();
    }
}
