using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Omnius.Lxna.Components.Internal.Helpers
{
    public class PathHelperTest
    {
        [Fact]
        public void PathTest()
        {
            Assert.Equal("/", PathHelper.Normalize(@"\"));
        }
    }
}
