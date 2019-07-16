using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lxna.Core.Contents.Internal;
using Lxna.Messages;
using Omnix.Base;
using Omnix.Network;

namespace Lxna.Core.Contents
{
    public sealed class ContentExplorer : ServiceBase
    {
        private readonly LxnaOptions _options;
        private readonly ThumbnailCacheStorage _thumbnailCacheStorage;

        private ServiceStateType _state = ServiceStateType.Stopped;

        public ContentExplorer(LxnaOptions options)
        {
            _options = options;
            _thumbnailCacheStorage = new ThumbnailCacheStorage(_options);
        }

        public IEnumerable<LxnaContentClue> GetContentClues(OmniAddress? baseAddress, CancellationToken token = default)
        {
            var result = new List<LxnaContentClue>();

            if (baseAddress is null)
            {
                foreach (var drivePath in Directory.GetLogicalDrives())
                {
                    result.Add(new LxnaContentClue(LxnaContentType.Directory, drivePath[0].ToString()));
                }
            }
            else
            {
                if (FileSystemPathConverter.TryDecoding(baseAddress, out var basePath))
                {
                    try
                    {
                        foreach (var directoryPath in Directory.EnumerateDirectories(basePath, "*", SearchOption.TopDirectoryOnly))
                        {
                            result.Add(new LxnaContentClue(LxnaContentType.Directory, Path.GetFileName(directoryPath)));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }

                    try
                    {
                        foreach (var filePath in Directory.EnumerateFiles(basePath, "*", SearchOption.TopDirectoryOnly))
                        {
                            result.Add(new LxnaContentClue(LxnaContentType.File, Path.GetFileName(filePath)));
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }
                }
            }

            return result;
        }

        public IEnumerable<LxnaThumbnail> GetThumnails(OmniAddress address, int width, int height, LxnaThumbnailFormatType formatType, LxnaThumbnailResizeType resizeType, CancellationToken token = default)
        {
            return _thumbnailCacheStorage.GetThumnails(address, width, height, formatType, resizeType, token);
        }

        public void ReadContent(OmniAddress address, long position, Span<byte> buffer, CancellationToken token = default)
        {
            if (!FileSystemPathConverter.TryDecoding(address, out var path))
            {
                throw new ArgumentException();
            }

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fileStream.Seek(position, SeekOrigin.Begin);
                fileStream.Read(buffer);
            }
        }

        protected override async ValueTask OnInitializeAsync()
        {

        }

        protected override async ValueTask OnStartAsync()
        {
            this.StateType = ServiceStateType.Starting;

            this.StateType = ServiceStateType.Running;
        }

        protected override async ValueTask OnStopAsync()
        {
            this.StateType = ServiceStateType.Stopping;

            this.StateType = ServiceStateType.Stopped;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
               
            }
        }
    }
}
