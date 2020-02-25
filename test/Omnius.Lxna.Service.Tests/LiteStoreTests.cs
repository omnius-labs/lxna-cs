using System;
using System.IO;
using System.Threading.Tasks;
using Omnius.Core;
using Xunit;

namespace Omnius.Lxna.Service
{
    public class LiteStoreTests
    {
        [Fact]
        public async ValueTask ReadWriteTest()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "LiteStoreTests_ReadWriteTest");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var liteStore = await LiteStore.Factory.CreateAsync(path, BytesPool.Shared);
        }
    }
}
