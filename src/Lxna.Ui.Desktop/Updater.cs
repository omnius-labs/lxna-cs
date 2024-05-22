using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Core.Base;
using Core.Base.Helpers;

namespace Lxna.Ui.Desktop;

public class Updater : AsyncDisposableBase
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
    private const string Url = "https://docs.omnius-labs.com/releases/lxna.json";

    private Task _task = Task.CompletedTask;
    private CancellationTokenSource _cts = new();

    public static async ValueTask<Updater> CreateAsync()
    {
        var result = new Updater();
        return result;
    }

    internal Updater()
    {
        _task = this.BackgroundDownloadAsync();
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _cts.Cancel();
        await _task;
    }

    private async Task BackgroundDownloadAsync()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            await ReleasedZipDownloader.TryDownloadAsync(basePath, _cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }

    public bool TryExecute()
    {
        return UpdaterLauncher.TryLaunchUpdater(Directory.GetCurrentDirectory());
    }
}

static class ReleasedZipDownloader
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly HttpClient _httpClient = new HttpClient();
    private const string Url = "https://docs.omnius-labs.com/releases/lxna.json";

    public static async ValueTask<bool> TryDownloadAsync(string basePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var dirPath = Path.Combine(basePath, "../", "update", "zip");
            DirectoryHelper.CreateDirectory(dirPath);

            var env = GetEnvironment();
            var (targetVersionStr, downloadUrl) = await GetVersionAndDownloadUrlAsync(env, cancellationToken);
            var fileName = Path.GetFileName(downloadUrl);
            var targetVersion = Version.Parse(targetVersionStr);
            var currentVersion = GetCurrentVersion();

            if (currentVersion >= targetVersion)
            {
                return false;
            }

            var destFilePath = Path.Combine(dirPath, fileName);
            await DownloadFileAsync(downloadUrl, destFilePath, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return false;
        }
    }

    private static async ValueTask DownloadFileAsync(string url, string destFilePath, CancellationToken cancellationToken = default)
    {
        using var stream = await _httpClient.GetStreamAsync(url, cancellationToken);
        using var fileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
    }

    private static Version GetCurrentVersion()
    {
        var versionString = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        return Version.TryParse(versionString, out var version) ? version : new Version();
    }

    private static async ValueTask<(string, string)> GetVersionAndDownloadUrlAsync(string env, CancellationToken cancellationToken = default)
    {
        using var stream = await _httpClient.GetStreamAsync(Url, cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var version = doc.RootElement.GetProperty("version").GetString() ?? throw new InvalidOperationException("Version not found.");
        var downloadUrl = doc.RootElement.GetProperty("download_urls").GetProperty(env).GetString() ?? throw new InvalidOperationException("Download URL not found.");
        return (version, downloadUrl);
    }

    private static string GetEnvironment()
    {
        if (OperatingSystem.IsWindows()) return "win";
        if (OperatingSystem.IsLinux()) return "linux";
        throw new NotSupportedException("Unsupported operating system.");
    }
}

static class UpdaterLauncher
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static bool TryLaunchUpdater(string basePath)
    {
        var zipFilePath = GetZipFilePath(basePath);
        if (zipFilePath is null) return false;

        var updaterPath = CopyUpdater(basePath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            updaterPath += ".exe";
        }

        var processInfo = new ProcessStartInfo()
        {
            FileName = updaterPath,
            WorkingDirectory = Path.GetDirectoryName(updaterPath),
            Arguments = $"-b {Path.Combine(basePath, "../")}",
            UseShellExecute = false,
        };

        Process.Start(processInfo);

        return true;
    }

    private static string? GetZipFilePath(string basePath)
    {
        string dirPath = Path.Combine(basePath, "../", "update", "zip");
        return Directory.GetFiles(dirPath, "*.zip").FirstOrDefault();
    }

    private static string CopyUpdater(string basePath)
    {
        var destPath = Path.Combine(basePath, "../", "update", "updater");
        if (Directory.Exists(destPath)) Directory.Delete(destPath, true);
        Directory.CreateDirectory(destPath);

        var srcPath = Path.Combine(basePath, "updater");

        CopyFiles(srcPath, destPath);

        return Path.Combine(destPath, "Lxna.Updater");
    }

    private static void CopyFiles(string sourcePath, string destinationPath)
    {
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath, StringComparison.InvariantCulture));
        }

        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, destinationPath, StringComparison.InvariantCulture), true);
        }
    }
}
