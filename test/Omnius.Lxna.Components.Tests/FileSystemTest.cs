using Xunit;
using Omnius.Core.UnitTestToolkit;
using Omnius.Core;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.Storages.Models;

namespace Omnius.Lxna.Components
{
    public class FileSystemTest
    {
        [Fact]
        public async Task NestedArchiveFileTest()
        {
            using var deleter = FixtureFactory.GenTempDirectory(out var tempDirPath);
            var testRootDirPath = Path.Combine(TestEnvironment.GetBasePath(), "Data/NestedArchiveFileTest");

            var storageOptions = new LxnaStorageOptions { RootDirectoryPath = testRootDirPath, TempDirectoryPath = tempDirPath };
            var storage = await LxnaStorage.CreateAsync(BytesPool.Shared, storageOptions);

            var rootDirs = await storage.FindDirectoriesAsync();
            using var rootDir = rootDirs.Single();
            Assert.Equal(new NestedPath(testRootDirPath), rootDir.LogicalPath);

            var firstFiles = await rootDir.FindFilesAsync();
            using var firstZipFile = firstFiles.Single();
            Assert.True(firstZipFile.Attributes.HasFlag(Storages.FileAttributes.Archive));
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip")), firstZipFile.LogicalPath);

            using var firstZipDir = await firstZipFile.TryConvertToDirectoryAsync();
            Assert.NotNull(firstZipDir);
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), ""), firstZipDir.LogicalPath);

            var firstZipDirInDirs = await firstZipDir.FindDirectoriesAsync();
            using var firstZipDirInDir = firstZipDirInDirs.Single();
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1"), firstZipDirInDir.LogicalPath);

            var secondFiles = await firstZipDirInDir.FindFilesAsync();
            using var secondZipFile = secondFiles.Single();
            Assert.True(secondZipFile.Attributes.HasFlag(Storages.FileAttributes.Archive));
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1/2.zip"), secondZipFile.LogicalPath);

            using var secondZipDir = await secondZipFile.TryConvertToDirectoryAsync();
            Assert.NotNull(secondZipDir);
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1/2.zip", ""), secondZipDir.LogicalPath);

            var secondZipDirInDirs = await secondZipDir.FindDirectoriesAsync();
            using var secondZipDirInDir = secondZipDirInDirs.Single();
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1/2.zip", "2"), secondZipDirInDir.LogicalPath);

            var thirdFiles = await secondZipDirInDir.FindFilesAsync();
            using var thirdZipFile = thirdFiles.Single();
            Assert.True(thirdZipFile.Attributes.HasFlag(Storages.FileAttributes.Archive));
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1/2.zip", "2/3.zip"), thirdZipFile.LogicalPath);

            using var thirdZipDir = await thirdZipFile.TryConvertToDirectoryAsync();
            Assert.NotNull(thirdZipDir);
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1/2.zip", "2/3.zip", ""), thirdZipDir.LogicalPath);

            var thirdZipDirInDirs = await thirdZipDir.FindDirectoriesAsync();
            using var thirdZipDirInDir = thirdZipDirInDirs.Single();
            Assert.Equal(new NestedPath(Path.Combine(testRootDirPath, "1.zip"), "1/2.zip", "2/3.zip", "3"), thirdZipDirInDir.LogicalPath);
        }
    }
}