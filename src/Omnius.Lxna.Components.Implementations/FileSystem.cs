using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Extensions;
using Omnius.Lxna.Components.Internal.Helpers;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public sealed class FileSystem : AsyncDisposableBase, IFileSystem
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly HashSet<string> _archiveFileExtensionList = new HashSet<string>() { ".zip", ".rar", ".7z" };

        private readonly IArchiveFileExtractorFactory _archiveFileExtractorFactory;
        private readonly string _tempDirPath;
        private readonly IBytesPool _bytesPool;

        private readonly Dictionary<NestedPath, ArchiveFileExtractorStatus> _cacheMap = new();

        private readonly object _lockObject = new();

        internal sealed class FileSystemFactory : IFileSystemFactory
        {
            public async ValueTask<IFileSystem> CreateAsync(FileSystemOptions options)
            {
                var result = new FileSystem(options);
                await result.InitAsync();

                return result;
            }
        }

        public static IFileSystemFactory Factory { get; } = new FileSystemFactory();

        internal FileSystem(FileSystemOptions options)
        {
            _archiveFileExtractorFactory = options.ArchiveFileExtractorFactory ?? throw new ArgumentException($"{nameof(options.ArchiveFileExtractorFactory)} is null");
            _tempDirPath = options.TemporaryDirectoryPath ?? Path.Combine(Path.GetTempPath(), "FileSystem");
            _bytesPool = options.BytesPool ?? BytesPool.Shared;
        }

        internal async ValueTask InitAsync()
        {
        }

        protected override async ValueTask OnDisposeAsync()
        {
        }

        public async ValueTask<bool> ExistsFileAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                var result = await this.ExistsPhysicalFileAsync(path.Values[0], cancellationToken);
                return result;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.ExistsFileAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }

        private async ValueTask<bool> ExistsPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
        {
            return File.Exists(path);
        }

        public async ValueTask<bool> ExistsDirectoryAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                var result = await this.ExistsPhysicalDirectoryAsync(path.Values[0], cancellationToken);
                return result;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.ExistsDirectoryAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }

        private async ValueTask<bool> ExistsPhysicalDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            return Directory.Exists(path);
        }

        public async ValueTask<DateTime> GetFileLastWriteTimeAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                var result = await this.GetPhysicalFileLastWriteTimeAsync(path.Values[0], cancellationToken);
                return result;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.GetFileLastWriteTimeAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }

        public async ValueTask<DateTime> GetPhysicalFileLastWriteTimeAsync(string path, CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LastAccessTimeUtc;
        }

        public async ValueTask<IEnumerable<NestedPath>> FindDirectoriesAsync(NestedPath? path = null, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                var result = await this.FindRootPhysicalDirectoriesAsync(cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalDirectoriesAsync(path.Values[0], cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.FindDirectoriesAsync(path.Values[^1], cancellationToken);
                var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).ToList();
                list.Sort();
                return list;
            }
        }

        private async ValueTask<IEnumerable<string>> FindRootPhysicalDirectoriesAsync(CancellationToken cancellationToken = default)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Directory.GetLogicalDrives().ToArray();
            }
            else
            {
                return new string[] { "/" };
            }
        }

        private async ValueTask<IEnumerable<string>> FindPhysicalDirectoriesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetDirectories(path).Select(n => PathHelper.Normalize(n));
        }

        public async ValueTask<IEnumerable<NestedPath>> FindDirectoriesAndArchiveFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                var result = await this.FindRootPhysicalDirectoriesAsync(cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else if (path.Values.Count == 1)
            {
                var (dirs, files) = await this.FindPhysicalDirectoriesAndArchiveFilesAsync(path.Values[0], cancellationToken);
                var list = dirs.Select(n => new NestedPath(new[] { n })).Union(files.Select(n => new NestedPath(new[] { n, "" }))).ToList();
                list.Sort();
                return list;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var (dirs, files) = await archiveFileExtractor.FindDirectoriesAndArchiveFilesAsync(path.Values[^1], cancellationToken);
                var list = dirs.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).Union(files.Select(n => new NestedPath(archiveFilePath.Values.Append(n).Append("").ToArray()))).ToList();
                list.Sort();
                return list;
            }
        }

        private async ValueTask<(IEnumerable<string>, IEnumerable<string>)> FindPhysicalDirectoriesAndArchiveFilesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return (Enumerable.Empty<string>(), Enumerable.Empty<string>());
            }

            var dirs = new List<string>();
            var files = new List<string>();

            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                if (entry.Attributes.HasFlag(FileAttributes.Directory))
                {
                    dirs.Add(PathHelper.Normalize(entry.FullName));
                }
                else if (_archiveFileExtensionList.Contains(entry.Extension))
                {
                    files.Add(PathHelper.Normalize(entry.FullName));
                }
            }

            return (dirs, files);
        }

        public async ValueTask<IEnumerable<NestedPath>> FindFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalFileAsync(path.Values[0], cancellationToken);
                var list = result.Select(n => new NestedPath(new[] { n })).ToList();
                list.Sort();
                return list;
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.FindFilesAsync(path.Values[^1], cancellationToken);
                var list = result.Select(n => new NestedPath(archiveFilePath.Values.Append(n).ToArray())).ToList();
                list.Sort();
                return list;
            }
        }

        private async ValueTask<IEnumerable<string>> FindPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(path).Select(n => PathHelper.Normalize(n));
        }

        public async ValueTask<Stream> GetFileStreamAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                return await this.GetPhysicalFileStreamAsync(path.Values[0], cancellationToken);
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.GetFileStreamAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }

        private async ValueTask<Stream> GetPhysicalFileStreamAsync(string path, CancellationToken cancellationToken = default)
        {
            var fileStream = new FileStream(path, FileMode.Open);
            return fileStream;
        }

        public async ValueTask<long> GetFileSizeAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                return await this.GetPhysicalFileSizeAsync(path.Values[0], cancellationToken);
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.GetFileSizeAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }

        public async ValueTask<long> GetPhysicalFileSizeAsync(string path, CancellationToken cancellationToken = default)
        {
            return new FileInfo(path).Length;
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                return new PhysicalFileOwner(path.Values[0]);
            }
            else
            {
                var archiveFilePath = new NestedPath(path.Values.ToArray()[..^1]);
                var archiveFileExtractor = await this.GetArchiveFileExtractorAsync(archiveFilePath, cancellationToken);
                var result = await archiveFileExtractor.ExtractFileAsync(path.Values[^1], cancellationToken);
                return result;
            }
        }

        private class PhysicalFileOwner : AsyncDisposableBase, IFileOwner
        {
            public PhysicalFileOwner(string path)
            {
                this.Path = path;
            }

            public string Path { get; }

            protected override async ValueTask OnDisposeAsync()
            {
            }
        }

        private async ValueTask<IArchiveFileExtractor> GetArchiveFileExtractorAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            if (path.Values.Count == 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(path.Values)} is empty");
            }

            if (path.Values.Count == 1)
            {
                return await this.CreateArchiveFileExtractorAsync(path, null);
            }

            var result = await this.CreateArchiveFileExtractorAsync(path, null);

            for (int i = 1; i < path.Values.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileOwner = await result.ExtractFileAsync(path.Values[i], cancellationToken);
                result = await this.CreateArchiveFileExtractorAsync(new NestedPath(path.Values.ToArray()[..i]), fileOwner);
            }

            return result;
        }

        private async ValueTask<IArchiveFileExtractor> CreateArchiveFileExtractorAsync(NestedPath nestedPath, IFileOwner? fileOwner)
        {
            lock (_lockObject)
            {
                if (_cacheMap.TryGetValue(nestedPath, out var status))
                {
                    return status.ArchiveFileExtractor;
                }
            }

            var option = new ArchiveFileExtractorOptions()
            {
                ArchiveFilePath = fileOwner?.Path ?? nestedPath.Values[0],
                TemporaryDirectoryPath = _tempDirPath,
                BytesPool = _bytesPool,
            };
            var archiveFileExtractor = await _archiveFileExtractorFactory.CreateAsync(option);

            lock (_lockObject)
            {
                var status = new ArchiveFileExtractorStatus()
                {
                    ArchiveFileExtractor = archiveFileExtractor,
                    FileOwner = fileOwner,
                };
                _cacheMap.TryAdd(nestedPath, status);
            }

            return archiveFileExtractor;
        }

        private sealed class ArchiveFileExtractorStatus
        {
            public IArchiveFileExtractor ArchiveFileExtractor { get; init; } = null!;

            public IFileOwner? FileOwner { get; init; }
        }
    }
}
