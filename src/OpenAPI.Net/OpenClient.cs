using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public sealed class OpenClient: IOpenClient
    {
        private TcpClient _tcpClient;

        private SslStream _sslStream;

        public OpenClient(string host, int port)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;
        }

        public string Host { get; }
        public int Port { get; }

        public async Task Connect()
        {
            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            var stream = _tcpClient.GetStream();

            _sslStream = new SslStream(stream, false);

            await _sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _tcpClient.Dispose();

            _tcpClient = null;
            _sslStream = null;
        }
    }
}
