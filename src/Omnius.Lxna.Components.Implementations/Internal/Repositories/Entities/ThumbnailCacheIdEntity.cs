
using System;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components.Internal.Repositories.Entities
{
    public record ThumbnailCacheIdEntity
    {
        public string? FilePath { get; set; }
        public ThumbnailResizeType ThumbnailResizeType { get; set; }
        public ThumbnailFormatType ThumbnailFormatType { get; set; }
        public int ThumbnailWidth { get; set; }
        public int ThumbnailHeight { get; set; }
    }
}
