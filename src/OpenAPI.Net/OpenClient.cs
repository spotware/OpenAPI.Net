using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public sealed class OpenClient : IOpenClient
    {
        private TcpClient _tcpClient;

        private SslStream _sslStream;

        private CancellationTokenSource _listenerCancellationTokenSource;

        private Action<Exception> _onListenerException;

        public OpenClient(string host, int port, Action<Exception> onListenerException)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;

            _onListenerException = onListenerException ?? throw new ArgumentNullException(nameof(onListenerException));
        }

        public string Host { get; }
        public int Port { get; }

        public bool IsDisposed { get; private set; }

        public bool IsListening {get; private set; }

        public async Task Connect()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            var stream = _tcpClient.GetStream();

            _sslStream = new SslStream(stream, false);

            await _sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);

            _listenerCancellationTokenSource = new CancellationTokenSource();

            StartListening(_listenerCancellationTokenSource.Token);
        }

        private async void StartListening(CancellationToken cancelationToken)
        {
            IsListening = true;

            try
            {
                var lengthArray = new byte[sizeof(int)];

                var readBytes = 0;

                do
                {
                    readBytes += await _sslStream.ReadAsync(lengthArray, readBytes, lengthArray.Length - readBytes, cancelationToken)
                        .ConfigureAwait(false);
                }
                while (readBytes < lengthArray.Length);

                var length = BitConverter.ToInt32(lengthArray.Reverse().ToArray(), 0);

                if (length <= 0) return;

                var message = new byte[length];

                readBytes = 0;

                do
                {
                    readBytes += await _sslStream.ReadAsync(message, readBytes, message.Length - readBytes, cancelationToken)
                    .ConfigureAwait(false);
                }
                while (readBytes < length);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                IsListening = false;

                var onListenerException = _onListenerException;

                if (onListenerException != null)
                {
                    onListenerException.Invoke(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                IsListening = false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            _listenerCancellationTokenSource.Cancel();

            while (IsListening) await Task.Delay(1000);

            _tcpClient?.Dispose();

            _onListenerException = null;
            _listenerCancellationTokenSource = null;
            _tcpClient = null;
            _sslStream = null;
        }
    }
}