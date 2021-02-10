using System;
using Xunit;
using Omnius.Core.UnitTestToolkit;
using Omnius.Core;
using System.Threading.Tasks;
using Omnius.Lxna.Components.Models;
using System.IO;
using System.Linq;
using Omnius.Lxna.Components.Internal.Helpers;

namespace Omnius.Lxna.Components
{
    public class FileSystemTest
    {
        [Fact]
        public async Task NestedArchiveFileTest()
        {
            using var deleter = FixtureFactory.GenTempDirectory(out var tempDirPath);
            var testDir = Path.Combine(TestEnvironment.GetBasePath(), "Data/NestedArchiveFileTest");

            var fileSystemOptions = new FileSystemOptions()
            {
                ArchiveFileExtractorFactory = ArchiveFileExtractor.Factory,
                TemporaryDirectoryPath = tempDirPath,
                BytesPool = BytesPool.Shared,
            };
            await using (var fileSystem = await FileSystem.Factory.CreateAsync(fileSystemOptions))
            {
                var rank1 = await fileSystem.FindArchiveFilesAsync(new NestedPath(new[] { testDir }));
                Assert.Single(rank1);
                Assert.Equal(new NestedPath(new[] { PathHelper.Normalize(Path.Combine(testDir, "1.zip")), "" }), rank1.First());

                var rank1_1 = await fileSystem.FindDirectoriesAsync(rank1.First());
                Assert.Single(rank1_1);
                Assert.Equal(new NestedPath(new[] { PathHelper.Normalize(Path.Combine(testDir, "1.zip")), "1" }), rank1_1.First());

                var rank2 = await fileSystem.FindArchiveFilesAsync(rank1_1.First());
                Assert.Single(rank2);
                Assert.Equal(new NestedPath(new[] { PathHelper.Normalize(Path.Combine(testDir, "1.zip")), "1/2.zip", "" }), rank2.First());

                var rank2_2 = await fileSystem.FindDirectoriesAsync(rank2.First());
                Assert.Single(rank2_2);
                Assert.Equal(new NestedPath(new[] { PathHelper.Normalize(Path.Combine(testDir, "1.zip")), "1/2.zip", "2" }), rank2_2.First());

                var rank3 = await fileSystem.FindArchiveFilesAsync(rank2_2.First());
                Assert.Single(rank3);
                Assert.Equal(new NestedPath(new[] { PathHelper.Normalize(Path.Combine(testDir, "1.zip")), "1/2.zip", "2/3.zip", "" }), rank3.First());

                var rank3_3 = await fileSystem.FindDirectoriesAsync(rank3.First());
                Assert.Single(rank3_3);
                Assert.Equal(new NestedPath(new[] { PathHelper.Normalize(Path.Combine(testDir, "1.zip")), "1/2.zip", "2/3.zip", "3" }), rank3_3.First());
            }
        }
    }
}
