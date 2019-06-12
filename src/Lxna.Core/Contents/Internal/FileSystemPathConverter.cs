using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Omnix.Network;

namespace Lxna.Core.Contents.Internal
{
    internal static class FileSystemPathConverter
    {
        public static bool TryEncoding(string path, out OmniAddress? omniAddress)
        {
            omniAddress = null;

            if (path.Length < 3)
            {
                return false;
            }

            if (!(('a' <= path[0] && path[0] <= 'z') || ('A' <= path[0] && path[0] <= 'Z')))
            {
                return false;
            }

            if (!(path[1] == ':' && path[2] == '\\'))
            {
                return false;
            }

            var sb = new StringBuilder();
            sb.Append("/" + path[0] + "/");
            sb.Append(path.Substring(3).Replace('\\', '/'));

            omniAddress = new OmniAddress(sb.ToString());
            return true;
        }

        public static bool TryDecoding(OmniAddress omniAddress, out string? path)
        {
            path = null;

            var sections = omniAddress.Parse();

            if (sections.Length == 0)
            {
                return false;
            }

            if (sections[0].Length != 1)
            {
                return false;
            }

            var driveName = sections[0][0];

            if (!(('a' <= driveName && driveName <= 'z') || ('A' <= driveName && driveName <= 'Z')))
            {
                return false;
            }

            var sb = new StringBuilder();
            sb.Append(driveName + @":\");
            sb.Append(string.Join('\\', sections.Skip(1)));

            path = sb.ToString();
            return true;
        }
    }
}
