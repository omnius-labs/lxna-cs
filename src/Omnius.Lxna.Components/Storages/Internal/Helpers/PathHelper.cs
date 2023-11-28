using System.Text;

namespace Omnius.Lxna.Components.Storages.Internal.Helpers;

internal static unsafe class PathHelper
{
    public static string Combine(string path1, string path2)
    {
        var sb = new StringBuilder();

        sb.Append(path1.AsSpan().TrimEnd('/'));
        if (sb.Length > 0) sb.Append('/');

        sb.Append(path2.AsSpan().Trim('/'));

        return sb.ToString();
    }

    public static string Normalize(string path)
    {
        return path.Replace(@"\", "/", StringComparison.InvariantCulture);
    }

    public static bool IsParentDirectory(string parentDirPath, string targetPath)
    {
        var parentDirSpan = parentDirPath.AsSpan().TrimEnd('/');
        var targetSpan = targetPath.AsSpan().TrimEnd('/');

        if ((parentDirSpan.Length + 1) > targetSpan.Length) return false;

        if (parentDirSpan.Length > 0)
        {
            if (!targetSpan.StartsWith(parentDirSpan) || targetPath[parentDirSpan.Length] != '/') return false;

            targetSpan = targetSpan[(parentDirSpan.Length + 1)..];
        }

        return !targetSpan.Contains('/');
    }

    public static IEnumerable<string> ExtractDirectories(string path)
    {
        var results = new List<string>();
        var span = path.AsSpan().TrimEnd('/');

        for (; ; )
        {
            var offset = span.LastIndexOf('/');
            if (offset < 0) break;

            span = span[0..offset];

            var dirPath = new string(span);
            results.Add(dirPath);
        }

        return results;
    }
}
