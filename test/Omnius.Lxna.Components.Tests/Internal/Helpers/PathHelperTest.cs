using System;
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

        [Fact]
        public void IsCurrentDirectoryTest()
        {
            Assert.False(PathHelper.IsCurrentDirectory("", ""));
            Assert.True(PathHelper.IsCurrentDirectory("", "a"));
            Assert.True(PathHelper.IsCurrentDirectory("", "a/"));
            Assert.False(PathHelper.IsCurrentDirectory("", "a/b"));
            Assert.False(PathHelper.IsCurrentDirectory("", "a/b/"));
            Assert.False(PathHelper.IsCurrentDirectory("", "a/b/c"));
            Assert.False(PathHelper.IsCurrentDirectory("", "a/b/c/"));

            Assert.False(PathHelper.IsCurrentDirectory("aaa", "aaa"));
            Assert.True(PathHelper.IsCurrentDirectory("aaa", "aaa/bbb"));
            Assert.True(PathHelper.IsCurrentDirectory("aaa", "aaa/bbb/"));
            Assert.False(PathHelper.IsCurrentDirectory("aaa", "aaa/bbb/ccc"));
            Assert.False(PathHelper.IsCurrentDirectory("aaa", "aaa/bbb/ccc/"));
            Assert.False(PathHelper.IsCurrentDirectory("aaa", "bbbbb"));
            Assert.False(PathHelper.IsCurrentDirectory("aaa", "aaabb"));
        }

        [Fact]
        public void ExtractDirectoriesTest()
        {
            Assert.Equal(Array.Empty<string>(), PathHelper.ExtractDirectories("a"));
            Assert.Equal(new[] { "a" }, PathHelper.ExtractDirectories("a/b"));
            Assert.Equal(new[] { "a/b", "a" }, PathHelper.ExtractDirectories("a/b/c"));
        }
    }
}
