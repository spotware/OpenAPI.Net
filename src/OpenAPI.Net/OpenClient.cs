using Google.Protobuf;
using OpenAPI.Net.Exceptions;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public class OpenClient : IOpenClient
    {
        private readonly TimeSpan _heartbeatInerval;

        private ProtoHeartbeatEvent _heartbeatEvent = new ProtoHeartbeatEvent();

        private SemaphoreSlim _streamWriteSemaphoreSlim = new SemaphoreSlim(1, 1);

        private ConcurrentDictionary<int, IObserver<IMessage>> _observers = new ConcurrentDictionary<int, IObserver<IMessage>>();

        private TcpClient _tcpClient;

        private SslStream _sslStream;

        private IDisposable _listenerDisposable;

        private IDisposable _heartbeatDisposable;

        public OpenClient(string host, int port, TimeSpan heartbeatInerval)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;

            _heartbeatInerval = heartbeatInerval;
        }

        ~OpenClient() => Dispose(false);

        public string Host { get; }
        public int Port { get; }

        public bool IsDisposed { get; private set; }

        public DateTimeOffset LastSentMessageTime { get; private set; }

        public async Task Connect()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            _tcpClient = new TcpClient { LingerState = new LingerOption(true, 10) };

            await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            var stream = _tcpClient.GetStream();

            _sslStream = new SslStream(stream, false);

            await _sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);

            _listenerDisposable = Observable.DoWhile(Observable.FromAsync(Read), () => !IsDisposed)
                .Where(iMessage => iMessage != null)
                .Subscribe(OnNext);
            _heartbeatDisposable = Observable.Interval(_heartbeatInerval).DoWhile(() => !IsDisposed)
                .Subscribe(x => SendHeartbeat());
        }

        public IDisposable Subscribe(IObserver<IMessage> observer)
        {
            if (!_observers.Values.Contains(observer))
            {
                _observers.TryAdd(_observers.Count, observer);
            }

            return Disposable.Create(() => OnObserverDispose(observer));
        }

        public Task SendMessage<T>(T message, ProtoPayloadType payloadType, string clientMsgId = null) where T :
            IMessage
        {
            var protoMessage = MessageFactory.GetMessage(message, payloadType, clientMsgId);

            return SendMessage(protoMessage);
        }

        public Task SendMessage<T>(T message, ProtoOAPayloadType payloadType, string clientMsgId = null) where T :
            IMessage
        {
            var protoMessage = MessageFactory.GetMessage(message, payloadType, clientMsgId);

            return SendMessage(protoMessage);
        }

        public async Task SendMessage(ProtoMessage message)
        {
            try
            {
                var messageByte = message.ToByteArray();

                var length = BitConverter.GetBytes(messageByte.Length).Reverse().ToArray();

                LastSentMessageTime = DateTime.Now;

                await Write(messageByte, length);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            IsDisposed = true;

            if (disposing)
            {
                _heartbeatDisposable.Dispose();
                _listenerDisposable.Dispose();

                _tcpClient.Dispose();

                _streamWriteSemaphoreSlim.Dispose();
            }

            OnCompleted();
        }

        private async Task<IMessage> Read(CancellationToken cancelationToken)
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

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

                var data = new byte[length];

                readBytes = 0;

                do
                {
                    var count = data.Length - readBytes;

                    readBytes += await _sslStream.ReadAsync(data, readBytes, count).ConfigureAwait(false);
                }
                while (readBytes < length);

                return MessageFactory.GetMessage(data);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                var readException = new ReadException(ex);

                OnError(readException);
            }

            return null;
        }

        private async Task Write(byte[] messageByte, byte[] length)
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            var isSemaphoreEntered = await _streamWriteSemaphoreSlim.WaitAsync(TimeSpan.FromMinutes(1));

            if (!isSemaphoreEntered) throw new TimeoutException(ErrorMessages.SemaphoreEnteryTimedOut);

            try
            {
                await _sslStream.WriteAsync(length, 0, length.Length).ConfigureAwait(false);

                await _sslStream.WriteAsync(messageByte, 0, messageByte.Length).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var writeException = new WriteException(ex);

                OnError(writeException);
            }
            finally
            {
                _streamWriteSemaphoreSlim.Release();
            }
        }

        private async void SendHeartbeat()
        {
            if (DateTimeOffset.Now - LastSentMessageTime < _heartbeatInerval) return;

            await SendMessage(_heartbeatEvent, ProtoPayloadType.HeartbeatEvent).ConfigureAwait(false);
        }

        private void OnObserverDispose(IObserver<IMessage> observer)
        {
            var (observerKey, observerToRemove) = _observers.FirstOrDefault(iObserver => iObserver.Value == observer);

            if (observerToRemove == observer)
            {
                _observers.TryRemove(observerKey, out _);
            }
        }

        private void OnNext(IMessage message)
        {
            foreach (var (_, observer) in _observers)
            {
                observer.OnNext(message);
            }
        }

        private void OnError(Exception exception)
        {
            if (_observers.Count == 0) throw exception;

            foreach (var (_, observer) in _observers)
            {
                observer.OnError(exception);
            }
        }

        private void OnCompleted()
        {
            foreach (var (_, observer) in _observers)
            {
                observer.OnCompleted();
            }
        }
    }
}