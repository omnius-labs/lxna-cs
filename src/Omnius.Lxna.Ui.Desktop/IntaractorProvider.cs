using Omnius.Core;
using Omnius.Lxna.Ui.Desktop.Configuration;

namespace Omnius.Lxna.Ui.Desktop;

public interface IIntaractorProvider
{
}

public class IntaractorProvider : AsyncDisposableBase, IIntaractorProvider
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    private readonly AppConfig _appConfig;
    private readonly IBytesPool _bytesPool;

    private readonly AsyncLock _asyncLock = new();

    public IntaractorProvider(AppConfig appConfig, IBytesPool bytesPool)
    {
        _appConfig = appConfig;
        _bytesPool = bytesPool;
    }

    protected override async ValueTask OnDisposeAsync()
    {
    }
}
