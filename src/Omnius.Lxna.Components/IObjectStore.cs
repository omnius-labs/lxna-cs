using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Omnius.Core;
using Omnius.Core.Serialization.RocketPack;

namespace Omnius.Lxna.Components
{
    public interface IObjectStoreFactory
    {
        ValueTask<IObjectStore> CreateAsync(string configPath, IBytesPool bytesPool);
    }

    public interface IObjectStore : IAsyncDisposable
    {
        IAsyncEnumerable<string> GetKeysAsync(CancellationToken cancellationToken = default);
        ValueTask DeleteKeyAsync(string key, CancellationToken cancellationToken = default);

        ValueTask<T> ReadAsync<T>(string key, CancellationToken cancellationToken = default) where T : IRocketPackObject<T>;
        ValueTask WriteAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : IRocketPackObject<T>;
    }
}
