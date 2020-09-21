namespace Omnius.Lxna.Deamon.Internal
{
    using System.Linq;
    using Omnius.Core.Network;
    using Omnius.Lxna.Deamon.Models;
    using Models = Omnius.Lxna.Components.Models;

    internal class OptionsGenerator
    {
        private static OmniAddress? CreateAddress(string? value)
        {
            if (value is null) return null;
            return new OmniAddress(value);
        }

        public static Models.TcpConnectorOptions GenTcpConnectorOptions(LxnaConfig config)
        {
            static Models.TcpConnectingOptions GenTcpConnectingOptions(LxnaConfig config)
            {
                return new Models.TcpConnectingOptions(
                    config.Connectors?.TcpConnector?.Connecting?.Enabled ?? false,
                    new Models.TcpProxyOptions(
                        config.Connectors?.TcpConnector?.Connecting?.Proxy?.Type switch
                        {
                            LxnaConfig.TcpProxyType.HttpProxy => Models.TcpProxyType.HttpProxy,
                            LxnaConfig.TcpProxyType.Socks5Proxy => Models.TcpProxyType.Socks5Proxy,
                            _ => Models.TcpProxyType.Unknown,
                        },
                        CreateAddress(config?.Connectors?.TcpConnector?.Connecting?.Proxy?.Address)
                    )
                );
            }

            static Models.TcpAcceptingOptions GenTcpAcceptingOptions(LxnaConfig config)
            {
                return new Models.TcpAcceptingOptions(
                    config.Connectors.TcpConnector.Accepting.Enabled,
                    config.Connectors.TcpConnector.Accepting.ListenAddresses.Select(n => new OmniAddress(n)).ToArray(),
                    config.Connectors.TcpConnector.Accepting.UseUpnp
                );
            }

            static Models.BandwidthOptions GenBandwidthOptions(LxnaConfig config)
            {
                return new Models.BandwidthOptions(
                    config.Connectors.TcpConnector.Bandwidth.MaxSendBytesPerSeconds,
                    config.Connectors.TcpConnector.Bandwidth.MaxReceiveBytesPerSeconds
                );
            }

            return new Models.TcpConnectorOptions(
                GenTcpConnectingOptions(config),
                GenTcpAcceptingOptions(config),
                GenBandwidthOptions(config)
            );
        }
    }
}
