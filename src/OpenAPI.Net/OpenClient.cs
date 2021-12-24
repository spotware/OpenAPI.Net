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
using System.Threading.Channels;

namespace OpenAPI.Net
{
    public sealed class OpenClient : IDisposable, IObservable<IMessage>
    {
        private readonly TimeSpan _heartbeatInerval;

        private readonly ProtoHeartbeatEvent _heartbeatEvent = new ProtoHeartbeatEvent();

        private readonly Channel<ProtoMessage> _messagesChannel = Channel.CreateUnbounded<ProtoMessage>();

        private readonly ConcurrentDictionary<int, IObserver<IMessage>> _observers = new ConcurrentDictionary<int, IObserver<IMessage>>();

        private readonly CancellationTokenSource _messagesCancellationTokenSource = new CancellationTokenSource();

        private readonly TimeSpan _requestDelay;

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
        /// <param name="maxRequestPerSecond">The maximum number of requests client will send per second</param>
        /// <param name="useWebSocket">By default OpenClient uses raw TCP connection, if you want to use web socket instead set this parameter to true</param>
        public OpenClient(string host, int port, TimeSpan heartbeatInerval, int maxRequestPerSecond = 40, bool useWebSocket = false)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));

            if (port < 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;

            _heartbeatInerval = heartbeatInerval;
            MaxRequestPerSecond = maxRequestPerSecond;
            _requestDelay = TimeSpan.FromMilliseconds(1000 / MaxRequestPerSecond);
            UseWebSocket = useWebSocket;
        }

        /// <summary>
        /// The API endpoint host that the current client is connected to
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The API host port that current client is connected to
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// The maximum number of requests client will send per second
        /// </summary>
        public int MaxRequestPerSecond { get; }

        /// <summary>
        /// If client is connected via websocket then this will return True, otherwise False
        /// </summary>
        public bool UseWebSocket { get; }

        /// <summary>
        /// If client is disposed then this will return True, otherwise False
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// If client stream is completed without any error then it will return True, otherwise False
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// If there was any error (exception) on client stream then this will return True, otherwise False
        /// </summary>
        public bool IsTerminated { get; private set; }

        /// <summary>
        /// The time client sent its last message
        /// </summary>
        public DateTimeOffset LastSentMessageTime { get; private set; }

        /// <summary>
        /// Connects to the API based on you specified method (websocket or TCP)
        /// </summary>
        /// <returns>Task</returns>
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

            _ = StartSendingMessages(_messagesCancellationTokenSource.Token);

            _heartbeatDisposable = Observable.Interval(_heartbeatInerval).DoWhile(() => !IsDisposed)
                .Subscribe(x => SendHeartbeat());
        }

        /// <summary>
        /// Connects to API by using websocket
        /// </summary>
        /// <returns>Task</returns>
        private async Task ConnectWebScoket()
        {
            var hostUri = new Uri($"wss://{Host}:{Port}");

            _websocketClient = new WebsocketClient(hostUri, new Func<ClientWebSocket>(() => new ClientWebSocket()))
            {
                IsTextMessageConversionEnabled = false,
                ReconnectTimeout = null,
                IsReconnectionEnabled = false,
                ErrorReconnectTimeout = null
            };

            _webSocketMessageReceivedDisposable = _websocketClient.MessageReceived.Select(msg => ProtoMessage.Parser.ParseFrom(msg.Binary))
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

        /// <summary>
        /// Connects to API by using a TCP client
        /// </summary>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Subscribe to client incoming messages
        /// </summary>
        /// <param name="observer">The observer that will receive the messages</param>
        /// <returns>IDisposable</returns>
        public IDisposable Subscribe(IObserver<IMessage> observer)
        {
            ThrowObjectDisposedExceptionIfDisposed();

            _observers.AddOrUpdate(observer.GetHashCode(), observer, (key, oldObserver) => observer);

            return Disposable.Create(() => OnObserverDispose(observer));
        }

        /// <summary>
        /// This method will insert your message on messages queue, it will not send the message instantly
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="message">Message</param>
        /// <param name="payloadType">Message Payload Type (ProtoPayloadType)</param>
        /// <param name="clientMsgId">The client message ID (optional)</param>
        /// <returns>Task</returns>
        public async Task SendMessage<T>(T message, ProtoPayloadType payloadType, string clientMsgId = null) where T :
            IMessage
        {
            var protoMessage = MessageFactory.GetMessage(message, payloadType, clientMsgId);

            await SendMessage(protoMessage);
        }

        /// <summary>
        /// This method will insert your message on messages queue, it will not send the message instantly
        /// </summary>
        /// <typeparam name="T">Message Type</typeparam>
        /// <param name="message">Message</param>
        /// <param name="payloadType">Message Payload Type (ProtoOAPayloadType)</param>
        /// <param name="clientMsgId">The client message ID (optional)</param>
        /// <returns>Task</returns>
        public async Task SendMessage<T>(T message, ProtoOAPayloadType payloadType, string clientMsgId = null) where T :
            IMessage
        {
            var protoMessage = MessageFactory.GetMessage(message, payloadType, clientMsgId);

            await SendMessage(protoMessage);
        }

        /// <summary>
        /// This method will insert your message on messages queue, it will not send the message instantly
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Task</returns>
        public async Task SendMessage(ProtoMessage message)
        {
            await _messagesChannel.Writer.WriteAsync(message);
        }

        /// <summary>
        /// This method will send the passed message instantly
        /// If the client was already in use it will terminate the stream and you will get an error on your observers
        /// Use the other SendMessage methods to avoid issues with multiple threads trying to send message at the same time
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>Task</returns>
        public async Task SendMessageInstant(ProtoMessage message)
        {
            try
            {
                var messageByte = message.ToByteArray();

                var length = BitConverter.GetBytes(messageByte.Length);

                Array.Reverse(length);

                if (UseWebSocket)
                {
                    _websocketClient.Send(messageByte);
                }
                else
                {
                    await WriteTcp(messageByte, length);
                }

                LastSentMessageTime = DateTimeOffset.Now;
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>
        /// This method will keep reading the messages channel and then it will send the read message
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that will cancel the reading</param>
        /// <returns>Task</returns>
        private async Task StartSendingMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (await _messagesChannel.Reader.WaitToReadAsync(cancellationToken) && IsDisposed is false && IsTerminated is false)
                {
                    while (_messagesChannel.Reader.TryRead(out var message))
                    {
                        var timeElapsedSinceLastMessageSent = DateTimeOffset.Now - LastSentMessageTime;

                        if (timeElapsedSinceLastMessageSent < _requestDelay)
                        {
                            await Task.Delay(_requestDelay - timeElapsedSinceLastMessageSent);
                        }

                        await SendMessageInstant(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        /// <summary>
        /// Disposes the client and stops all running operations
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            _heartbeatDisposable?.Dispose();
            _listenerDisposable?.Dispose();

            _messagesCancellationTokenSource.Cancel();

            _messagesChannel.Writer.TryComplete();

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

            if (!IsTerminated) OnCompleted();
        }

        /// <summary>
        /// This method will read the TCP stream for incoming messages
        /// </summary>
        /// <returns>Task<ProtoMessage></returns>
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

        /// <summary>
        /// Writes the message bytes to TCP stream
        /// </summary>
        /// <param name="messageByte"></param>
        /// <param name="length"></param>
        /// <returns>Task</returns>
        private async Task WriteTcp(byte[] messageByte, byte[] length)
        {
            ThrowObjectDisposedExceptionIfDisposed();

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
        }

        /// <summary>
        /// Sends heartbeat to API for keeping the connection alive
        /// </summary>
        private async void SendHeartbeat()
        {
            if (DateTimeOffset.Now - LastSentMessageTime < _heartbeatInerval) return;

            await SendMessage(_heartbeatEvent, ProtoPayloadType.HeartbeatEvent).ConfigureAwait(false);
        }

        /// <summary>
        /// This method will be called if the web socket connection got disconnected
        /// </summary>
        /// <param name="disconnectionInfo">The disconnection info</param>
        private void OnWebSocketDisconnectionHappened(DisconnectionInfo disconnectionInfo)
        {
            disconnectionInfo.CancelReconnection = true;

            OnError(disconnectionInfo.Exception);
        }

        /// <summary>
        /// Removes the disposed ovserver from client observers collection
        /// </summary>
        /// <param name="observer"></param>
        private void OnObserverDispose(IObserver<IMessage> observer)
        {
            _observers.TryRemove(observer.GetHashCode(), out _);
        }

        /// <summary>
        /// Calls each observer OnNext with the message
        /// </summary>
        /// <param name="protoMessage">Message</param>
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

        /// <summary>
        /// Disposes the client and then calls each observer OnError after an exception thrown
        /// </summary>
        /// <param name="exception">Exception</param>
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

        /// <summary>
        /// Completes each observer by calling their OnCompleted method
        /// </summary>
        private void OnCompleted()
        {
            IsCompleted = true;

            foreach (var (_, observer) in _observers)
            {
                observer.OnCompleted();
            }
        }

        /// <summary>
        /// Throws ObjectDisposedException if the client was disposed
        /// </summary>
        private void ThrowObjectDisposedExceptionIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
        }
    }
}