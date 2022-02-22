using System.Net;
using System.Net.Http;

namespace Chaincase.Common.Services
{
    public class Socks5HttpClientHandler : HttpClientHandler
    {
        public Socks5HttpClientHandler(Config config)
        {
            if (config.TorSocks5EndPoint is IPEndPoint endpoint)
            {
                this.Proxy = new WebProxy($"socks5://{endpoint.Address}:{endpoint.Port}");
            }
            else if (config.TorSocks5EndPoint is DnsEndPoint endpoint2)
            {
                this.Proxy = new WebProxy($"socks5://{endpoint2.Host}:{endpoint2.Port}");
            }
        }
    }
}