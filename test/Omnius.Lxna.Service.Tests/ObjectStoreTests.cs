using System;
using System.IO;
using System.Threading.Tasks;
using Omnius.Core;
using Xunit;

namespace Omnius.Lxna.Service
{
    public class ObjectStoreTests
    {
        [Fact]
        public async ValueTask ReadWriteTest()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "ObjectStoreTests_ReadWriteTest");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var objectStore = await ObjectStore.Factory.CreateAsync(path, BytesPool.Shared);
        }
    }
}
