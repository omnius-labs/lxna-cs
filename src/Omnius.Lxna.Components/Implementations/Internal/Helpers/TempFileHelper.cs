using System;
using System.Buffers;
using System.IO;
using Omnius.Core.Serialization;

namespace Omnius.Lxna.Components.Internal.Helpers;

internal static unsafe class TempFileHelper
{
    private static readonly Random _random = new();
    private static readonly Lazy<Base16> _base16 = new(() => new Base16(), true);

    public static FileStream GenStream(string destinationDirectoryPath, string extension)
    {
        var buffer = new byte[32];

        for (; ; )
        {
            _random.NextBytes(buffer);
            var randomText = _base16.Value.BytesToString(new ReadOnlySequence<byte>(buffer));
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
