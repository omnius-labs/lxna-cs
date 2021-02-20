using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Collections;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public interface IThumbnailGeneratorFactory
    {
        ValueTask<IThumbnailGenerator> CreateAsync(ThumbnailGeneratorOptions options);
    }

    public class ThumbnailGeneratorOptions
    {
        public string? ConfigDirectoryPath { get; init; }

        public uint Concurrency { get; init; }

        public IFileSystem? FileSystem { get; init; }

        public IBytesPool? BytesPool { get; init; }
    }

    public interface IThumbnailGenerator : IAsyncDisposable
    {
        ValueTask<ThumbnailGeneratorGetThumbnailResult> GetThumbnailAsync(NestedPath filePath, ThumbnailGeneratorGetThumbnailOptions options, bool cacheOnly = false, CancellationToken cancellationToken = default);
    }

    public readonly struct ThumbnailGeneratorGetThumbnailOptions
    {
        public ThumbnailGeneratorGetThumbnailOptions(int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, TimeSpan minInterval, int maxImageCount)
        {
            this.Width = width;
            this.Height = height;
            this.FormatType = formatType;
            this.ResizeType = resizeType;
            this.MinInterval = minInterval;
            this.MaxImageCount = maxImageCount;
        }

        public int Width { get; }

        public int Height { get; }

        public ThumbnailFormatType FormatType { get; }

        public ThumbnailResizeType ResizeType { get; }

        public TimeSpan MinInterval { get; }

        public int MaxImageCount { get; }
    }

    public enum ThumbnailGeneratorResultStatus
    {
        Unknown,
        Succeeded,
        Failed,
    }

    public readonly struct ThumbnailGeneratorGetThumbnailResult
    {
        public ThumbnailGeneratorGetThumbnailResult(ThumbnailGeneratorResultStatus status, IEnumerable<ThumbnailContent>? contents = null)
        {
            this.Status = status;
            this.Contents = new ReadOnlyListSlim<ThumbnailContent>(contents?.ToArray() ?? Array.Empty<ThumbnailContent>());
        }

        public ThumbnailGeneratorResultStatus Status { get; }

        public ReadOnlyListSlim<ThumbnailContent> Contents { get; }
    }
}
