using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Omnius.Lxna.Launcher.Helpers;

namespace Omnius.Lxna.Launcher;

public class Downloader
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private static readonly HttpClient _httpClient = new HttpClient();
    private const string Url = "https://docs.omnius-labs.com/releases/lxna.json";

    public static async void Download()
    {
        var basePath = Directory.GetCurrentDirectory();
        string updatePath = Path.Combine(basePath, "update");
        DirectoryHelper.TryCreate(updatePath);

        await TryDownloadReleasedZipAsync(updatePath);
    }

    private static async ValueTask<bool> TryDownloadReleasedZipAsync(string destDir)
    {
        try
        {
            var env = GetEnvironment();
            var (targetVersionStr, downloadUrl) = await GetVersionAndDownloadUrlAsync(env);
            var fileName = Path.GetFileName(downloadUrl);
            var targetVersion = Version.Parse(targetVersionStr);
            var currentVersion = GetCurrentVersion();

            if (currentVersion >= targetVersion)
            {
                return false;
            }

            var destFilePath = Path.Combine(destDir, fileName);
            await DownloadFileAsync(downloadUrl, destFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            return false;
        }
    }

    private static async ValueTask DownloadFileAsync(string url, string destFilePath)
    {
        using var stream = await _httpClient.GetStreamAsync(url);
        using var fileStream = new FileStream(destFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
    }

    private static Version GetCurrentVersion()
    {
        var versionString = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
        return Version.TryParse(versionString, out var version) ? version : new Version();
    }

    private static async ValueTask<(string, string)> GetVersionAndDownloadUrlAsync(string env)
    {
        using var stream = await _httpClient.GetStreamAsync(Url);
        using var doc = await JsonDocument.ParseAsync(stream);
        var version = doc.RootElement.GetProperty("version").GetString() ?? throw new InvalidOperationException("Version not found.");
        var downloadUrl = doc.RootElement.GetProperty("urls").GetProperty(env).GetString() ?? throw new InvalidOperationException("Download URL not found.");
        return (version, downloadUrl);
    }

    private static string GetEnvironment()
    {
        if (OperatingSystem.IsWindows()) return "win";
        if (OperatingSystem.IsLinux()) return "linux";

        throw new NotSupportedException("Unsupported operating system.");
    }
}
