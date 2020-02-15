using System;
using System.Collections.Generic;
using System.Linq;
using Omnius.Core.Collections;

namespace Omnius.Lxna.Service
{
    public enum ThumbnailGeneratorResultStatus
    {
        Unknown,
        Succeeded,
        Failed,
    }

    public readonly struct ThumbnailGeneratorResult
    {
        public ThumbnailGeneratorResult(ThumbnailGeneratorResultStatus status, ThumbnailMetadata? metadata = null, IEnumerable< ThumbnailContent>? contents = null)
        {
            this.Status = status;
            this.Metadata = metadata;
            this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
        }

        public ThumbnailGeneratorResultStatus Status { get; }
        public ThumbnailMetadata? Metadata { get; }
        public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
    }
}
