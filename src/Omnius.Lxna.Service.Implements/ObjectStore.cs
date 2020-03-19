using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;
using Omnius.Core;
using Omnius.Core.Io;
using Omnius.Core.Serialization.RocketPack;
using Omnius.Core.Serialization.RocketPack.Helpers;

namespace Omnius.Lxna.Service
{
    public class ObjectStore : AsyncDisposableBase, IObjectStore
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _configPath;
        private readonly IBytesPool _bytesPool;

        private LiteDatabase _liteDatabase;

        private readonly AsyncLock _asyncLock = new AsyncLock();

        internal sealed class ObjectStoreFactory : IObjectStoreFactory
        {
            public async ValueTask<IObjectStore> CreateAsync(string configPath, IBytesPool bytesPool)
            {
                var result = new ObjectStore(configPath, bytesPool);
                await result.InitAsync();

                return result;
            }
        }

        public static IObjectStoreFactory Factory { get; } = new ObjectStoreFactory();

        internal ObjectStore(string configPath, IBytesPool bytesPool)
        {
            _configPath = configPath;
            _bytesPool = bytesPool;
        }

        internal async ValueTask InitAsync()
        {
            _liteDatabase = new LiteDatabase(Path.Combine(_configPath, "lite.db"));
        }

        protected override async ValueTask OnDisposeAsync()
        {
            _liteDatabase.Dispose();
        }

        public async IAsyncEnumerable<string> GetKeysAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using (await _asyncLock.LockAsync())
            {
                foreach (var key in _liteDatabase.FileStorage.FindAll().Select(n => n.Id.Substring(1)))
                {
                    yield return key;
                }
            }
        }

        public async ValueTask DeleteKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            using (await _asyncLock.LockAsync())
            {
                _liteDatabase.FileStorage.Delete("$" + key);
            }
        }

        public async ValueTask<T> ReadAsync<T>(string key, CancellationToken cancellationToken = default) where T : IRocketPackObject<T>
        {
            using (await _asyncLock.LockAsync())
            {
                return await Task.Run(() =>
                {
                    var liteFileInfo = _liteDatabase.FileStorage.FindById("$" + key);

                    if (liteFileInfo == null)
                    {
                        return IRocketPackObject<T>.Empty;
                    }

                    using (var inStream = liteFileInfo.OpenRead())
                    {
                        return RocketPackHelper.StreamToMessage<T>(inStream);
                    }
                });
            }
        }

        public async ValueTask WriteAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : IRocketPackObject<T>
        {
            using (await _asyncLock.LockAsync())
            {
                await Task.Run(() =>
                {
                    using (var recyclableMemoryStream = new RecyclableMemoryStream(_bytesPool))
                    {
                        RocketPackHelper.MessageToStream(value, recyclableMemoryStream);
                        recyclableMemoryStream.Seek(0, SeekOrigin.Begin);

                        _liteDatabase.FileStorage.Upload("$" + key, Path.GetFileName(key), recyclableMemoryStream);
                    }
                });
            }
        }
    }
}
