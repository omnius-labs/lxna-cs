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
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public sealed class LxnaFileSystem : AsyncDisposableBase, ILxnaFileSystem
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IArchiveFileExtractor _archiveFileExtractor;
        private readonly IBytesPool _bytesPool;

        internal sealed class LxnaFileSystemFactory : ILxnaFileSystemFactory
        {
            public async ValueTask<ILxnaFileSystem> CreateAsync(LxnaFileSystemOptions options)
            {
                var result = new LxnaFileSystem(options);
                await result.InitAsync();

                return result;
            }
        }

        public static ILxnaFileSystemFactory Factory { get; } = new LxnaFileSystemFactory();

        internal LxnaFileSystem(LxnaFileSystemOptions options)
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

        public async ValueTask<IEnumerable<LxnaPath>> FindDirectoriesAsync(LxnaPath? path = null, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                var result = await this.FindRootPhysicalDirectoriesAsync(cancellationToken);
                return result.Select(n => new LxnaPath(new[] { n })).ToArray();
            }
            else if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalDirectoriesAsync(path.Values[0], cancellationToken);
                return result.Select(n => new LxnaPath(new[] { n })).ToArray();
            }
            else
            {
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await _archiveFileExtractor.ExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.FindDirectoriesAsync(archiveFile.Path, path.Values[^1], cancellationToken);
                return result.Select(n => new LxnaPath(basePaths.Append(n).ToArray())).ToArray();
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

            return Directory.GetDirectories(path);
        }

        public async ValueTask<IEnumerable<LxnaPath>> FindFilesAsync(LxnaPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                var result = await this.FindPhysicalFileAsync(path.Values[0], cancellationToken);
                return result.Select(n => new LxnaPath(new[] { n })).ToArray();
            }
            else
            {
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await _archiveFileExtractor.ExtractFileAsync(basePaths, cancellationToken);
                var result = await _archiveFileExtractor.FindFilesAsync(archiveFile.Path, path.Values[^1], cancellationToken);
                return result.Select(n => new LxnaPath(basePaths.Append(n).ToArray())).ToArray();
            }
        }

        private async ValueTask<IEnumerable<string>> FindPhysicalFileAsync(string path, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(path);
        }

        public async ValueTask<IMemoryOwner<byte>> ReadFileBytesAsync(LxnaPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            if (path.Values.Count == 1)
            {
                return await this.ReadPhysicalFileBytesAsync(path.Values[0], cancellationToken);
            }
            else
            {
                var basePaths = path.Values.ToArray()[..^1];
                using var archiveFile = await _archiveFileExtractor.ExtractFileAsync(basePaths, cancellationToken);
                return await _archiveFileExtractor.ReadFileBytesAsync(archiveFile.Path, path.Values[^1], cancellationToken);
            }
        }

        private async ValueTask<IMemoryOwner<byte>> ReadPhysicalFileBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            using var fileStream = new FileStream(path, FileMode.Open);
            var length = (int)fileStream.Length;
            var memoryOwner = _bytesPool.Memory.Rent(length).Shrink(length);
            await fileStream.ReadAsync(memoryOwner.Memory, cancellationToken);
            return memoryOwner;
        }

        public async ValueTask<long> GetFileSizeAsync(LxnaPath path, CancellationToken cancellationToken = default)
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
                using var archiveFile = await _archiveFileExtractor.ExtractFileAsync(basePaths, cancellationToken);
                return await _archiveFileExtractor.GetFileSizeAsync(archiveFile.Path, path.Values[^1], cancellationToken);
            }
        }

        public async ValueTask<long> GetPhysicalFileSizeAsync(string path, CancellationToken cancellationToken = default)
        {
            return new FileInfo(path).Length;
        }

        public async ValueTask<IFileOwner> ExtractFileAsync(LxnaPath path, CancellationToken cancellationToken = default)
        {
            if (path is null || path.Values.Count == 0)
            {
                throw new ArgumentException($"{nameof(path)} is invalid");
            }

            return await _archiveFileExtractor.ExtractFileAsync(path.Values, cancellationToken);
        }
    }
}
