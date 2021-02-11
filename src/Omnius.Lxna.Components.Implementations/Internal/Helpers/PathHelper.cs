using System;
using System.Collections.Generic;

namespace Omnius.Lxna.Components.Internal.Helpers
{
    internal static unsafe class PathHelper
    {
        public static string Normalize(string path)
        {
            return path.Replace(@"\", "/");
        }

        public static bool IsCurrentDirectory(string currentDirectoryPath, string targetPath)
        {
            var dirSpan = currentDirectoryPath.AsSpan().TrimEnd('/');
            var targetSpan = targetPath.AsSpan().TrimEnd('/');

            if ((dirSpan.Length + 1) > targetSpan.Length) return false;

            if (dirSpan.Length > 0)
            {
                if (!targetSpan.StartsWith(dirSpan) || targetPath[dirSpan.Length] != '/') return false;

                targetSpan = targetSpan[(dirSpan.Length + 1)..];
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
}
