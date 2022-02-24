using System.Buffers;
using Omnius.Core.Serialization;

namespace Omnius.Lxna.Components.Storage.Windows.Internal.Helpers;

internal static unsafe class FileHelper
{
    private static readonly Random _random = new();
    private static readonly Lazy<Base16> _base16 = new(() => new Base16(), true);

    public static FileStream GenTempFileStream(string tempDirectoryPath, string extension)
    {
        var buffer = new byte[32];

        for (; ; )
        {
            _random.NextBytes(buffer);
            var randomText = _base16.Value.BytesToString(new ReadOnlySequence<byte>(buffer));
            var tempFilePath = Path.Combine(tempDirectoryPath, randomText + extension);

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
