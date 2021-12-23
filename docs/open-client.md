# Creating/Connecting Client

The open client is the object that you will use to send/receive messages to API.

First add these usings:

```C#
	using OpenAPI.Net;
	using OpenAPI.Net.Auth;
	using OpenAPI.Net.Helpers;
	using System.Reactive.Linq;
	using System.Linq;
	using Google.Protobuf;
```

Then:

```C#
	// Mode can be either live or demo
	// If you want to access and work with live
	// trading accounts then use live otherwise use demo
	var host = ApiInfo.GetHost(mode);
	// You can set the maximum number of requests clients should send per second by using maxRequestPerSecond parameter (default: 40)
	// If you want to use web socket instead of TCP socket pass true for useWebSocket parameter of constructor
	// web socket allows you to use the Open API on static server-less sites like Blazor WASM environment 
    var client = new OpenClient(host, ApiInfo.Port, TimeSpan.FromSeconds(10));
```

The first parameter of OpenClient is host, this is the API host URL based on your selected mode, the second parameter is API host port, and the third one is a time span that will be used as interval for sending heartbeats to API server.

Use 10 or 15 seconds for sending heartbeats.

<strong>You don't have to send heartbeat to server manually, the client will send that and it will keep its connection alive</strong>

After you created the client you have to call its Connect method:

```C#
    await client.Connect();
```

Now client is connected to API server and you can send/receive messages.

The OpenClient (IOpenClient) implements IDisposable, so you can use it on a C# "using" block like file streams.

Once you finished your work with client don't forget to dispose it or if you just need it for sending/receiving few messages you can use it inside a using block:

```C#
	using (var client = new OpenClient(host, ApiInfo.Port, TimeSpan.FromSeconds(10))
	{
		await client.Connect();
		
		// Send message or subscribe to responses here
	}
```

Or you can:

```C#
	using var client = new OpenClient(host, ApiInfo.Port, TimeSpan.FromSeconds(10));
	await client.Connect();
```

The OpenClient class has some useful public properties you can use to check the state of client.

# Sending Messages

You can send all of the Open API supported messages easily via your OpenClient object, just create the message object and send it by calling client "SendMessage" method:

```C#
	// The first message you must send
    var applicationAuthReq = new ProtoOAApplicationAuthReq
    {
        ClientId = "",
		ClientSecret = "",
    };

    await _client.SendMessage(applicationAuthReq,
		ProtoOAPayloadType.ProtoOaApplicationAuthReq,
		"This is client message ID and its optional");
```

You have to provide the message payload type, you can use "ProtoOAPayloadType" or "ProtoPayloadType" enumeration types to get the message payload type based on your message type.

The last parameter of "SendMessage" method is client message ID, you don't have to pass it as the client message ID is optional, this message will be returned back on response.

All open API messages have their own classes, just create an object of message class and send it.

# Receiving Messages

The OpenClient (IOpenClient) class implements the RX "IObservable<IMessage>" interface, the "IMessage" is the base class of all Google Protobuf messages, this allows you to easily subscribe to any message type you want to receive:

```C#
	// Here I'm filtering out the ProtoHeartbeatEvent and subscribing
	// to all other message types
	// The OnError will be called if something went wrong
	var disposable = client.Where(iMessage =>  iMessage is not ProtoHeartbeatEvent)
		.Subscribe(OnMessageReceived, OnError);
```

A basic understanding of RX observable streams will help you a lot.

After you subscribed to a message it will return back an IDisposable object, to unsubscribe you have to dispose the returned IDisposable object.

To subscribe for an specific message type use Linq OfType extension method:

```C#
	// Here I'm subscribing to only ProtoOAErrorRes
	var disposable = client.OfType<ProtoOAErrorRes>().Subscribe(OnError);
```

You can use any of the Open API response or event messages to subscribe like above code snippet.

If you want to receive a message with a client message ID (clientMsgId) then you have to subscribe to "ProtoMessage" and then use the MessageFactory class to get the actual message:

```C#
	var disposable = client.OfType<ProtoMessage>().Subscribe(OnProtoMessage);
	
	private void OnProtoMessage(ProtoMessage protoMessage)
	{
		var clientMsgId = protoMessage.ClientMsgId;
		// Message factory can return null
		var message = MessageFactory.GetMessage(protoMessage);
	}
```

Client only stream a ProtoMessage if its ClientMsgId is set or it couldn't parse the actual message, otherwise it will not stream it.

# Handling Exceptions

If something went wrong or client lost connection to API you can get the thrown exception by subscribing to client stream OnError:

```C#
	// Here I'm subscribing to only ProtoOAErrorRes
	var disposable = client.Subscribe(_ => {}, OnError);
```

Now client will call and pass the exception to your OnError, also if your OnError method got triggered it means the client stream is terminated, based on RX guidelines you have to stop interacting with it, you don't have to dispose the client after it got terminated because the client will dispose itself on termination.

If you try to dispose a terminated client nothing will happen.

Client most probably will throw one of these exceptions types:

* ReadException: This exception type will be thrown if something went wrong during reading of client TCP stream
* ObserverException: This exception will be thrown if something went wrong during an observer (subscriber) OnNext method call, you can get the observer object via its Observer property

Check the above exceptions "InnerException" property to get the actual exception.

During call to any of client "SendMessage" methods you can expect one of these exceptions:

* WriteException: This exception will be thrown if something went wrong during writing on client TCP stream, check the innerExceptio for gettign the actual exception
* ObjectDisposedException: If you call send message of a disposed client then it will throw this excpetion

# Disposing Client

As mentioned the client object implements IDisposable interface, so you must dispose it after finishing your work.

To avoid calling dispose method several times you can check the client IsDisposed property and calling dispose method several times will not cause any issue.

If a client got terminated by an exception and it called the OnError of observers then it will dispose itself and you don't have to call the dispose method inside your OnError handler.

If the client is disposed without termination then it will call the observers OnCompleted handler.