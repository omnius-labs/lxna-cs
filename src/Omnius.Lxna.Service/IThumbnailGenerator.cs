using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Collections;
using Omnius.Core.Data;
using Omnius.Core.Network;

namespace Omnius.Lxna.Service
{
    public enum ThumbnailGeneratorGetResultStatus
    {
        Unknown,
        Succeeded,
        Failed,
    }

    public readonly struct ThumbnailGeneratorGetResult
    {
        public ThumbnailGeneratorGetResult(ThumbnailGeneratorGetResultStatus status, ThumbnailMetadata? metadata = null, IEnumerable<ThumbnailContent>? contents = null)
        {
            this.Status = status;
            this.Metadata = metadata;
            this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
        }

        public ThumbnailGeneratorGetResultStatus Status { get; }
        public ThumbnailMetadata? Metadata { get; }
        public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
    }

    public interface IThumbnailGeneratorFactory
    {
        ValueTask<IThumbnailGenerator> CreateAsync(string configPath, ILiteStoreFactory liteStoreFactory, IBytesPool bytesPool);
    }

    public interface IThumbnailGenerator : IAsyncDisposable
    {
        ValueTask<ThumbnailGeneratorGetResult> GetAsync(OmniPath omniPath, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken cancellationToken = default);
    }
}
