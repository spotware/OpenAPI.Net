using Google.Protobuf;
using System;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public interface IOpenClient : IDisposable, IObservable<IMessage>
    {
        public Task Connect();

        public Task SendMessage<T>(T message, ProtoPayloadType payloadType, string clientMsgId = null) where T : IMessage;

        public Task SendMessage<T>(T message, ProtoOAPayloadType payloadType, string clientMsgId = null) where T : IMessage;

        public Task SendMessage(ProtoMessage message);
    }
}