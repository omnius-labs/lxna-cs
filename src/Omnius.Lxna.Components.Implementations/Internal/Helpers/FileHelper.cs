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
        public static async ValueTask<FileStream> GenTempFileStreamAsync(string destinationDirectoryPath, Random random, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[32];
            var base16 = new Base16();

            for (; ; )
            {
                cancellationToken.ThrowIfCancellationRequested();
                random.NextBytes(buffer);
                var randomText = base16.BytesToString(buffer);
                var tempFilePath = Path.Combine(destinationDirectoryPath, randomText);

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
