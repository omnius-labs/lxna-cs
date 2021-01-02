using System;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components.Internal.Repositories.Entities
{
    public class ThumbnailCacheIdEntity
    {
        public NestedPathEntity? FilePath { get; init; }

        public ThumbnailResizeType ThumbnailResizeType { get; init; }

        public ThumbnailFormatType ThumbnailFormatType { get; init; }

        public int ThumbnailWidth { get; init; }

        public int ThumbnailHeight { get; init; }
    }
}
