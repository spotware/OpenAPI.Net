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
    public sealed class OpenClient : IDisposable, IObservable<IMessage>
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

        public string Host { get; }
        public int Port { get; }

        public bool IsDisposed { get; private set; }

        public bool IsCompleted { get; private set; }

        public bool IsTerminated { get; private set; }

        public DateTimeOffset LastSentMessageTime { get; private set; }

        public async Task Connect()
        {
            ThrowObjectDisposedExceptionIfDisposed();

            _tcpClient = new TcpClient { LingerState = new LingerOption(true, 10) };

            await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            var stream = _tcpClient.GetStream();

            _sslStream = new SslStream(stream, false);

            await _sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);

            _listenerDisposable = Observable.DoWhile(Observable.FromAsync(Read), () => !IsDisposed)
                .Where(message => message != null)
                .Subscribe(OnNext);
            _heartbeatDisposable = Observable.Interval(_heartbeatInerval).DoWhile(() => !IsDisposed)
                .Subscribe(x => SendHeartbeat());
        }

        public IDisposable Subscribe(IObserver<IMessage> observer)
        {
            ThrowObjectDisposedExceptionIfDisposed();

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
            if (IsDisposed) return;

            IsDisposed = true;

            _heartbeatDisposable?.Dispose();
            _listenerDisposable?.Dispose();

            _tcpClient?.Dispose();

            _streamWriteSemaphoreSlim?.Dispose();

            if (!IsTerminated) OnCompleted();
        }

        private async Task<ProtoMessage> Read()
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

                var data = new byte[length];

                readBytes = 0;

                do
                {
                    var count = data.Length - readBytes;

                    readBytes += await _sslStream.ReadAsync(data, readBytes, count).ConfigureAwait(false);
                }
                while (readBytes < length);

                return ProtoMessage.Parser.ParseFrom(data);
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
            ThrowObjectDisposedExceptionIfDisposed();

            bool isSemaphoreEntered = false;

            try
            {
                isSemaphoreEntered = await _streamWriteSemaphoreSlim.WaitAsync(TimeSpan.FromMinutes(1));

                if (!isSemaphoreEntered) throw new TimeoutException(ErrorMessages.SemaphoreEnteryTimedOut);

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
                if (isSemaphoreEntered) _streamWriteSemaphoreSlim.Release();
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

        private void OnNext(ProtoMessage protoMessage)
        {
            foreach (var (_, observer) in _observers)
            {
                try
                {
                    observer.OnNext(protoMessage);

                    var message = MessageFactory.GetMessage(protoMessage);

                    if (message != null) observer.OnNext(message);
                }
                catch (Exception ex)
                {
                    var observerException = new ObserverException(ex, observer);

                    OnError(observerException);
                }
            }
        }

        private void OnError(Exception exception)
        {
            IsTerminated = true;

            Dispose();

            foreach (var (_, observer) in _observers)
            {
                observer.OnError(exception);
            }
        }

        private void OnCompleted()
        {
            IsCompleted = true;

            foreach (var (_, observer) in _observers)
            {
                observer.OnCompleted();
            }
        }

        private void ThrowObjectDisposedExceptionIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
        }
    }
}