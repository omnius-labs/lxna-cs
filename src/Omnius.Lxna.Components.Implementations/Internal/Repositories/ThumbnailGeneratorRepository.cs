using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Nito.AsyncEx;
using Omnius.Core;
using Omnius.Core.Helpers;
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

            private readonly AsyncReaderWriterLock _asyncLock = new();

            public ThumbnailCachesRepository(LiteDatabase database, IBytesPool bytesPool)
            {
                _database = database;
                _bytesPool = bytesPool;
            }

            public ILiteStorage<ThumbnailCacheIdEntity> GetStorage() => _database.GetStorage<ThumbnailCacheIdEntity>("_thumbnail_cache_files", "_thumbnail_cache_chunks");

            public async Task<ThumbnailCache?> FindOneAsync(NestedPath filePath, int width, int height, ThumbnailResizeType resizeType, ThumbnailFormatType formatType)
            {
                await Task.Delay(1).ConfigureAwait(false);

                using (await _asyncLock.ReaderLockAsync())
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

                    using (var inStream = liteFileInfo.OpenRead())
                    {
                        return RocketPackHelper.StreamToMessage<ThumbnailCache>(inStream);
                    }
                }
            }

            public async Task InsertAsync(ThumbnailCache entity)
            {
                await Task.Delay(1).ConfigureAwait(false);

                using (await _asyncLock.WriterLockAsync())
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
                        throw new NotSupportedException();
                    }

                    try
                    {
                        using (var outStream = storage.OpenWrite(id, "-"))
                        {
                            RocketPackHelper.MessageToStream(entity, outStream);
                        }

                        if (!_database.Commit())
                        {
                            _logger.Error("failed to commit");
                            throw new NotSupportedException();
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
}
