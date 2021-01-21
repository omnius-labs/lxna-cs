using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core.Serialization;
using Omnius.Core.Serialization.Extensions;

namespace Omnius.Lxna.Components.Internal.Helpers
{
    internal static unsafe class FileHelper
    {
        private static readonly Lazy<Base16> _base16 = new(() => new Base16(), true);

        public static async ValueTask<FileStream> GenTempFileStreamAsync(string destinationDirectoryPath, string extension, Random random, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[32];

            for (; ; )
            {
                cancellationToken.ThrowIfCancellationRequested();

                random.NextBytes(buffer);
                var randomText = _base16.Value.BytesToString(buffer);
                var tempFilePath = Path.Combine(destinationDirectoryPath, randomText + extension);

                try
                {
                    var stream = new FileStream(tempFilePath, FileMode.CreateNew);
                    return stream;
                }
                catch (IOException)
                {
                    continue;
                }
            }
        }
    }
}
