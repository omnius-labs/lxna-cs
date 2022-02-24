using LiteDB;
using Omnius.Core;
using Omnius.Core.Helpers;
using Omnius.Core.RocketPack;
using Omnius.Lxna.Components.Thumbnail.Internal.Repositories.Entities;
using Omnius.Lxna.Components.Thumbnail.Internal.Models;
using Omnius.Lxna.Components.Storage.Models;
using Omnius.Lxna.Components.Thumbnail.Models;

namespace Omnius.Lxna.Components.Internal.Repositories;

internal sealed class ThumbnailGeneratorRepository : IDisposable
{
    private readonly LiteDatabase _database;

    private readonly IBytesPool _bytesPool;

    public ThumbnailGeneratorRepository(string filePath, IBytesPool bytesPool)
    {
        DirectoryHelper.CreateDirectory(Path.GetDirectoryName(filePath)!);

        _bytesPool = bytesPool;
        _database = new LiteDatabase(filePath);
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
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly LiteDatabase _database;
        private readonly IBytesPool _bytesPool;

        private readonly AsyncLock _asyncLock = new();

        public ThumbnailCachesRepository(LiteDatabase database, IBytesPool bytesPool)
        {
            _database = database;
            _bytesPool = bytesPool;
        }

        public ILiteStorage<ThumbnailCacheIdEntity> GetStorage() => _database.GetStorage<ThumbnailCacheIdEntity>("_thumbnail_cache_files", "_thumbnail_cache_chunks");

        public async ValueTask<ThumbnailCache?> FindOneAsync(NestedPath filePath, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
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
                if (liteFileInfo is null) return null;

                using (var inStream = liteFileInfo.OpenRead())
                {
                    return RocketMessage.FromStream<ThumbnailCache>(inStream);
                }
            }
        }

        public async ValueTask InsertAsync(ThumbnailCache entity)
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

                if (!_database.BeginTrans())
                {
                    _logger.Error("current thread already in a transaction");
                    throw new Exception();
                }

                try
                {
                    using (var outStream = storage.OpenWrite(id, "-"))
                    {
                        RocketMessage.ToStream(entity, outStream);
                    }

                    if (!_database.Commit())
                    {
                        _logger.Error("failed to commit");
                        throw new Exception();
                    }
                }
                catch (Exception e)
                {
                    _logger.Debug(e);
                    _database.Rollback();
                }
            }
        }
    }
}
