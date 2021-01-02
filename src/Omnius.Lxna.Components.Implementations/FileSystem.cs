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

        private readonly IArchiveFileExtractor _archiveFileExtractor;
        private readonly IBytesPool _bytesPool;

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
            _archiveFileExtractor = options.ArchiveFileExtractor ?? throw new ArgumentException($"{nameof(options.ArchiveFileExtractor)} is null");
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
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.ExistsFileAsync(archiveFile.Path, path.Values[^1], cancellationToken);
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
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.ExistsFileAsync(archiveFile.Path, path.Values[^1], cancellationToken);
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
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.GetFileLastWriteTimeAsync(archiveFile.Path, path.Values[^1], cancellationToken);
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
                return result.Select(n => new NestedPath(new[] { n })).ToArray();
            }
            else if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalDirectoriesAsync(path.Values[0], cancellationToken);
                return result.Select(n => new NestedPath(new[] { n })).ToArray();
            }
            else
            {
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.FindDirectoriesAsync(archiveFile.Path, path.Values[^1], cancellationToken);
                return result.Select(n => new NestedPath(basePaths.Append(n).ToArray())).ToArray();
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

        public async ValueTask<IEnumerable<NestedPath>> FindArchiveFilesAsync(NestedPath path, CancellationToken cancellationToken = default)
        {
            var results = await this.FindFilesAsync(path, cancellationToken);
            return results
                .Where(n => _archiveFileExtensionList.Contains(n.GetExtension()))
                .Select(n => NestedPath.Combine(n, string.Empty))
                .ToArray();
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
                return result.Select(n => new NestedPath(new[] { n })).ToArray();
            }
            else
            {
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.FindFilesAsync(archiveFile.Path, path.Values[^1], cancellationToken);
                return result.Select(n => new NestedPath(basePaths.Append(n).ToArray())).ToArray();
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
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                return await _archiveFileExtractor.GetPhysicalFileStreamAsync(archiveFile.Path, path.Values[^1], cancellationToken);
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
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await this.InternalExtractFileAsync(basePaths, cancellationToken);
                return await _archiveFileExtractor.GetFileSizeAsync(archiveFile.Path, path.Values[^1], cancellationToken);
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

            var extractedFile = await this.InternalExtractFileAsync(path.Values, cancellationToken);
            return extractedFile;
        }

        private class PhysicalFileOwner : IFileOwner
        {
            public PhysicalFileOwner(string path)
            {
                this.Path = path;
            }

            public string Path { get; }

            public void Dispose()
            {
            }
        }

        private async ValueTask<IFileOwner> InternalExtractFileAsync(IEnumerable<string> pathList, CancellationToken cancellationToken = default)
        {
            var values = pathList.ToList();

            if (values.Count == 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(pathList)} is empty");
            }

            if (values.Count == 1)
            {
                return new PhysicalFileOwner(values[0]);
            }

            var extractedFileOwners = new List<IFileOwner>();

            try
            {
                while (values.Count >= 2)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileOwner = await _archiveFileExtractor.ExtractFileAsync(values[0], values[1], cancellationToken);
                    extractedFileOwners.Add(fileOwner);
                    values.RemoveAt(0);
                    values[0] = fileOwner.Path;
                }

                return extractedFileOwners[^1];
            }
            finally
            {
                foreach (var fileOwner in extractedFileOwners.ToArray()[..^1])
                {
                    fileOwner.Dispose();
                }
            }
        }
    }
}
