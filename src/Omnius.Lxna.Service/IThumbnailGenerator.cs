using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Data;
using Omnius.Core.Network;

namespace Omnius.Lxna.Service
{
    public interface IThumbnailGeneratorFactory
    {
        ValueTask<IThumbnailGenerator> CreateAsync(string configPath, IOmniDatabaseFactory databaseFactory, IBytesPool bytesPool);
    }

    public interface IThumbnailGenerator : IAsyncDisposable
    {
        public static IThumbnailGeneratorFactory Factory { get; }

        ValueTask<ThumbnailGeneratorResult> GetThumnailAsync(OmniPath omniPath, int width, int height, ThumbnailFormatType formatType, ThumbnailResizeType resizeType, CancellationToken cancellationToken = default);
    }
}
