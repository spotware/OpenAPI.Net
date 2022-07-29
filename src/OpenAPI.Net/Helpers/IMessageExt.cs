using Google.Protobuf;
using OpenAPI.Net.Helpers;

namespace OpenAPI.Net.Helpers
{
    public interface IMessageExt : IMessage
    {
        public global::ProtoPayloadType PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
public sealed partial class ProtoHeartbeatEvent : IMessageExt { }
public sealed partial class ProtoErrorRes : IMessageExt { }