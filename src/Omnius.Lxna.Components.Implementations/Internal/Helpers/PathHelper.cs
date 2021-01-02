using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core.Serialization;
using Omnius.Core.Serialization.Extensions;

namespace Omnius.Lxna.Components.Internal.Helpers
{
    internal static unsafe class PathHelper
    {
        public static string Normalize(string path)
        {
            return path.Replace(@"\", "/");
        }
    }
}
