using System.Buffers;
using Omnius.Core.Serialization;

namespace Omnius.Lxna.Components.Storage.Internal.Helpers;

internal static unsafe class FileHelper
{
    private static readonly Random _random = new();
    private static readonly Lazy<Base58Btc> _base58Btc = new(() => new Base58Btc(), true);

    public static FileStream GenTempFileStream(string tempDirectoryPath, string extension)
    {
        var buffer = new byte[32];

        int count = 0;

        for (; ; )
        {
            _random.NextBytes(buffer);
            var randomText = _base58Btc.Value.BytesToString(new ReadOnlySequence<byte>(buffer));
            var tempFilePath = Path.Combine(tempDirectoryPath, randomText + extension);

            try
            {
                var stream = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite, 1);
                return stream;
            }
            catch (IOException)
            {
                if (count++ < 1000) continue;
                throw;
            }
        }
    }
}
