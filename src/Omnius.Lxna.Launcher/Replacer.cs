using System.Diagnostics;
using System.IO.Compression;
using CommandLine;
using Omnius.Lxna.Launcher.Helpers;

namespace Omnius.Lxna.Launcher;

public static class Replacer
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public static bool TryRun()
    {
        bool result = false;

        var parsedResult = CommandLine.Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs());
        parsedResult = parsedResult.WithParsed(options =>
        {
            // extract mode
            if (options.Mode == null && TryExtract())
            {
                _logger.Info("Extract");
                result = true;
                return;
            }

            // replace mode
            if (options.Mode == "replace" && TryReplace())
            {
                _logger.Info("Replace");
                result = true;
                return;
            }

            // cleanup
            Cleanup();
        });

        return result;
    }

    private static bool TryExtract()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            string updatePath = Path.Combine(basePath, "update");
            string newPath = Path.Combine(basePath, "new");

            var fileLock = new FileLock(Path.Combine(basePath, "lock"));

            using (fileLock.Lock(TimeSpan.FromSeconds(30)))
            {
                if (!Directory.Exists(updatePath)) return false;

                var zipFilePath = Directory.GetFiles(updatePath, "*.zip").FirstOrDefault();
                if (zipFilePath is null) return false;

                DirectoryHelper.CreateOrTruncate(newPath);
                ZipFile.ExtractToDirectory(zipFilePath, newPath);

                Directory.Delete(updatePath, true);

                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = Path.Combine(newPath, "Omnius.Lxna.Launcher"),
                    WorkingDirectory = newPath,
                    Arguments = "--mode replace"
                };
                Process.Start(processStartInfo);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Failed to Extract");
            return false;
        }
    }

    private static bool TryReplace()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            string sourcePath = Path.Combine(basePath, "../new");
            string destPath = Path.Combine(basePath, "../");
            string backupPath = Path.Combine(basePath, "../backup");

            var fileLock = new FileLock(Path.Combine(destPath, "lock"));

            using (fileLock.Lock(TimeSpan.FromSeconds(30)))
            {
                MoveFilesForReplace(destPath, backupPath);
                CopyFilesForReplace(sourcePath, destPath);

                var processStartInfo = new ProcessStartInfo()
                {
                    FileName = Path.Combine(destPath, "Omnius.Lxna.Launcher"),
                    WorkingDirectory = destPath,
                };
                Process.Start(processStartInfo);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Failed to Replace");
            return false;
        }
    }

    private static void MoveFilesForReplace(string sourcePath, string destinationPath)
    {
        DirectoryHelper.CreateOrTruncate(destinationPath);

        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly))
        {
            var dirName = Path.GetFileName(dirPath);
            if (dirName == "backup") continue;
            if (dirName == "logs") continue;
            if (dirName == "new") continue;
            if (dirName == "storage") continue;

            Directory.Move(dirPath, Path.Combine(destinationPath, dirName));
        }

        foreach (var filePath in Directory.GetFiles(sourcePath, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName == "lock") continue;

            File.Move(filePath, Path.Combine(destinationPath, fileName));
        }
    }

    private static void CopyFilesForReplace(string sourcePath, string destinationPath)
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

    private static void Cleanup()
    {
        try
        {
            var basePath = Directory.GetCurrentDirectory();
            string newPath = Path.Combine(basePath, "new");

            if (Directory.Exists(newPath)) Directory.Delete(newPath, true);
        }
        catch (Exception e)
        {
            _logger.Error(e, $"Failed to Cleanup");
        }
    }
}
