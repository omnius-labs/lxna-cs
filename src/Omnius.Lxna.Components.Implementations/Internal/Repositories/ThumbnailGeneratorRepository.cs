using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Core.Io;
using Omnius.Core.RocketPack.Helpers;
using Omnius.Lxna.Components.Internal.Models;
using Omnius.Lxna.Components.Internal.Repositories.Entities;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components.Internal.Repositories
{
    internal sealed class ThumbnailGeneratorRepository : IDisposable
    {
        private readonly LiteDatabase _database;

        private readonly IBytesPool _bytesPool;

        public ThumbnailGeneratorRepository(string path, IBytesPool bytesPool)
        {
            _bytesPool = bytesPool;

            _database = new LiteDatabase(path);
            this.ThumbnailCaches = new ThumbnailCachesRepository(_database, _bytesPool);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        public async ValueTask MigrateAsync(CancellationToken cancellationToken = default)
        {
        }

        public ThumbnailCachesRepository ThumbnailCaches { get; set; }

        public sealed class ThumbnailCachesRepository
        {
            private readonly LiteDatabase _database;
            private readonly IBytesPool _bytesPool;

            private readonly AsyncLock _asyncLock = new AsyncLock();

            public ThumbnailCachesRepository(LiteDatabase database, IBytesPool bytesPool)
            {
                _database = database;
                _bytesPool = bytesPool;
            }

            public ILiteStorage<ThumbnailCacheIdEntity> GetStorage() => _database.GetStorage<ThumbnailCacheIdEntity>("_thumbnail_cache_files", "_thumbnail_cache_chunks");

            public async Task<ThumbnailCache?> FindOneAsync(NestedPath filePath, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
            {
                await Task.Delay(1).ConfigureAwait(false);

                using (await _asyncLock.LockAsync())
                {
                    var id = new ThumbnailCacheIdEntity()
                    {
                        FilePath = NestedPathEntity.Import(filePath),
                        ThumbnailWidth = width,
                        ThumbnailHeight = height,
                        ThumbnailResizeType = resizeType,
                        ThumbnailFormatType = formatType,
                    };
                    var storage = this.GetStorage();

                    var liteFileInfo = storage.FindById(id);
                    if (liteFileInfo is null)
                    {
                        return null;
                    }

                    using var inStream = liteFileInfo.OpenRead();
                    return RocketPackHelper.StreamToMessage<ThumbnailCache>(inStream);
                }
            }

            public async Task InsertAsync(ThumbnailCache entity)
            {
                await Task.Delay(1).ConfigureAwait(false);

                using (await _asyncLock.LockAsync())
                {
                    var id = new ThumbnailCacheIdEntity()
                    {
                        FilePath = NestedPathEntity.Import(entity.FileMeta.Path),
                        ThumbnailWidth = (int)entity.ThumbnailMeta.Width,
                        ThumbnailHeight = (int)entity.ThumbnailMeta.Height,
                        ThumbnailResizeType = entity.ThumbnailMeta.ResizeType,
                        ThumbnailFormatType = entity.ThumbnailMeta.FormatType,
                    };
                    var storage = this.GetStorage();

                    if (storage.Exists(id))
                    {
                        return;
                    }

                    using var outStream = storage.OpenWrite(id, "-");
                    RocketPackHelper.MessageToStream(entity, outStream);
                }
            }
        }
    }
}
