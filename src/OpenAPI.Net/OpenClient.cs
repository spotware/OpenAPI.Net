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
using Websocket.Client;
using System.Net.WebSockets;
using System.Reactive.Concurrency;

namespace OpenAPI.Net
{
    public sealed class OpenClient : IDisposable, IObservable<IMessage>
    {
        private readonly TimeSpan _heartbeatInerval;

        private readonly ProtoHeartbeatEvent _heartbeatEvent = new ProtoHeartbeatEvent();

        private readonly SemaphoreSlim _streamWriteSemaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly ConcurrentDictionary<int, IObserver<IMessage>> _observers = new ConcurrentDictionary<int, IObserver<IMessage>>();

        private TcpClient _tcpClient;

        private WebsocketClient _websocketClient;

        private SslStream _sslStream;

        private IDisposable _listenerDisposable;

        private IDisposable _heartbeatDisposable;

        private IDisposable _webSocketDisconnectionHappenedDisposable;

        private IDisposable _webSocketMessageReceivedDisposable;

        /// <summary>
        /// Creates an instance of OpenClient which is not connected yet
        /// </summary>
        /// <param name="host">The host name of API endpoint</param>
        /// <param name="port">The host port number</param>
        /// <param name="heartbeatInerval">The time interval for sending heartbeats</param>
        /// <param name="useWebSocket">By default OpenClient uses raw TCP connection, if you want to use web socket instead set this parameter to true</param>
        public OpenClient(string host, int port, TimeSpan heartbeatInerval, bool useWebSocket = false)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;

            _heartbeatInerval = heartbeatInerval;
            UseWebSocket = useWebSocket;
        }

        public string Host { get; }
        public int Port { get; }
        public bool UseWebSocket { get; }
        public bool IsDisposed { get; private set; }

        public bool IsCompleted { get; private set; }

        public bool IsTerminated { get; private set; }

        public DateTimeOffset LastSentMessageTime { get; private set; }

        public async Task Connect()
        {
            ThrowObjectDisposedExceptionIfDisposed();

            if (UseWebSocket)
            {
                await ConnectWebScoket();
            }
            else
            {
                await ConnectTcp();
            }

            _heartbeatDisposable = Observable.Interval(_heartbeatInerval).DoWhile(() => !IsDisposed)
                .Subscribe(x => SendHeartbeat());
        }

        private async Task ConnectWebScoket()
        {
            var hostUri = new Uri($"wss://{Host}:{Port}");

            _websocketClient = new WebsocketClient(hostUri, new Func<ClientWebSocket>(() => new ClientWebSocket()))
            {
                IsTextMessageConversionEnabled = false,
                ErrorReconnectTimeout = null,
                IsReconnectionEnabled = false,
                ReconnectTimeout = null
            };

            _webSocketMessageReceivedDisposable = _websocketClient.MessageReceived.Select(msg => ProtoMessage.Parser.ParseFrom(msg.Binary))
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(OnNext);

            _webSocketDisconnectionHappenedDisposable = _websocketClient.DisconnectionHappened.Subscribe(OnWebSocketDisconnectionHappened);

            try
            {
                await _websocketClient.StartOrFail();
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private async Task ConnectTcp()
        {
            _tcpClient = new TcpClient { LingerState = new LingerOption(true, 10) };

            await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

            _sslStream = new SslStream(_tcpClient.GetStream(), false);

            await _sslStream.AuthenticateAsClientAsync(Host).ConfigureAwait(false);

            _listenerDisposable = Observable.DoWhile(Observable.FromAsync(ReadTcp), () => !IsDisposed)
                .Where(message => message != null)
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(OnNext);
        }

        public IDisposable Subscribe(IObserver<IMessage> observer)
        {
            ThrowObjectDisposedExceptionIfDisposed();

            _observers.AddOrUpdate(observer.GetHashCode(), observer, (key, oldObserver) => observer);

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

                var length = BitConverter.GetBytes(messageByte.Length);

                Array.Reverse(length);

                LastSentMessageTime = DateTime.Now;

                if (UseWebSocket)
                {
                    _websocketClient.Send(messageByte);
                }
                else
                {
                    await WriteTcp(messageByte, length);
                }
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

            if (UseWebSocket)
            {
                _webSocketMessageReceivedDisposable?.Dispose();

                _webSocketDisconnectionHappenedDisposable?.Dispose();

                _websocketClient?.Dispose();
            }
            else
            {
                _tcpClient?.Dispose();
            }

            _streamWriteSemaphoreSlim?.Dispose();

            if (!IsTerminated) OnCompleted();
        }

        private async Task<ProtoMessage> ReadTcp()
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

                Array.Reverse(lengthArray);

                var length = BitConverter.ToInt32(lengthArray, 0);

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

        private async Task WriteTcp(byte[] messageByte, byte[] length)
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
                if (isSemaphoreEntered && !IsDisposed) _streamWriteSemaphoreSlim.Release();
            }
        }

        private async void SendHeartbeat()
        {
            if (DateTimeOffset.Now - LastSentMessageTime < _heartbeatInerval) return;

            await SendMessage(_heartbeatEvent, ProtoPayloadType.HeartbeatEvent).ConfigureAwait(false);
        }

        private void OnWebSocketDisconnectionHappened(DisconnectionInfo disconnectionInfo)
        {
            disconnectionInfo.CancelReconnection = true;

            OnError(disconnectionInfo.Exception ?? new WebSocketException("Websocket got disconnected"));
        }

        private void OnObserverDispose(IObserver<IMessage> observer)
        {
            _observers.TryRemove(observer.GetHashCode(), out _);
        }

        private void OnNext(ProtoMessage protoMessage)
        {
            foreach (var (_, observer) in _observers)
            {
                try
                {
                    var message = MessageFactory.GetMessage(protoMessage);

                    if (protoMessage.HasClientMsgId || message == null) observer.OnNext(protoMessage);

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
            if (IsTerminated) return;

            IsTerminated = true;

            Dispose();

            foreach (var (_, observer) in _observers)
            {
                try
                {
                    observer.OnError(exception);
                }
                catch (Exception ex) when (ex == exception)
                {
                }
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