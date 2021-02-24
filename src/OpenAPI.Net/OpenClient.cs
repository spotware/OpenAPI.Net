using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public sealed class OpenClient : IOpenClient
    {
        private TcpClient _tcpClient;

        private SslStream _sslStream;

        private Action<Exception> _onListenerException;
        
        private IDisposable _listenerDisposable;

        private IDisposable _heartbeatDisposable;

        private readonly TimeSpan _heartbeatInerval;

        public OpenClient(string host, int port, Action<Exception> onListenerException, TimeSpan heartbeatInerval)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;

            _onListenerException = onListenerException ?? throw new ArgumentNullException(nameof(onListenerException));

            _heartbeatInerval = heartbeatInerval;
        }

        public string Host { get; }
        public int Port { get; }

        public bool IsDisposed { get; private set; }

        public async Task Connect()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            var stream = _tcpClient.GetStream();

            _sslStream = new SslStream(stream, false);

            await _sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);

            _listenerDisposable = Observable.DoWhile(Observable.FromAsync(Listen), () => !IsDisposed).Subscribe(OnMessageReceived);
            _heartbeatDisposable = Observable.Interval(_heartbeatInerval).DoWhile(() => !IsDisposed).Subscribe(x => StartHeartbeat());
        }

        private async Task<byte[]> Listen(CancellationToken cancelationToken)
        {
            try
            {
                var lengthArray = new byte[sizeof(int)];

                var readBytes = 0;

                do
                {
                    var count = lengthArray.Length - readBytes;

                    readBytes += await _sslStream.ReadAsync(lengthArray, readBytes, count).ConfigureAwait(false);
                }
                while (readBytes < lengthArray.Length);

                var length = BitConverter.ToInt32(lengthArray.Reverse().ToArray(), 0);

                if (length <= 0) return null;

                var message = new byte[length];

                readBytes = 0;

                do
                {
                    var count = message.Length - readBytes;

                    readBytes += await _sslStream.ReadAsync(message, readBytes, count).ConfigureAwait(false);
                }
                while (readBytes < length);

                return message;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _onListenerException?.Invoke(ex);
            }

            return null;
        }

        private void OnMessageReceived(byte[] message)
        {

        }

        private void StartHeartbeat()
        {

        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            _heartbeatDisposable?.Dispose();
            _listenerDisposable?.Dispose();

            await Task.Delay(1000);

            _tcpClient?.Dispose();

            _onListenerException = null;
            _tcpClient = null;
            _sslStream = null;

            _heartbeatDisposable = null;
            _listenerDisposable = null;
        }
    }
}