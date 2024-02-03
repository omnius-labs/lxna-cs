using LiteDB;
using Omnius.Core;
using Omnius.Core.Helpers;
using Omnius.Core.RocketPack;
using Omnius.Lxna.Components.Storages;
using Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories.Entities;

namespace Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories;

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
            using (await _asyncLock.LockAsync().ConfigureAwait(false))
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
                    return RocketMessageConverter.FromStream<ThumbnailCache>(inStream);
                }
            }
        }

        public async ValueTask InsertAsync(ThumbnailCache entity)
        {
            using (await _asyncLock.LockAsync().ConfigureAwait(false))
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
                    throw new Exception("current thread already in a transaction");
                }

                try
                {
                    using (var outStream = storage.OpenWrite(id, "-"))
                    {
                        RocketMessageConverter.ToStream(entity, outStream);
                    }

                    if (!_database.Commit())
                    {
                        throw new ThumbnailGeneratorRepositoryException("failed to commit");
                    }
                }
                catch (Exception)
                {
                    _database.Rollback();
                    throw;
                }
            }
        }
    }
}

[Serializable]
public class ThumbnailGeneratorRepositoryException : Exception
{
    public ThumbnailGeneratorRepositoryException() { }
    public ThumbnailGeneratorRepositoryException(string message) : base(message) { }
    public ThumbnailGeneratorRepositoryException(string message, Exception inner) : base(message, inner) { }
}
