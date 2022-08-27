using Google.Protobuf;
using OpenAPI.Net.Helpers;
using ProtoOA.Enums;

namespace OpenAPI.Net.Helpers
{
    internal interface IMessageExt : IMessage
    {
        public ProtoPayloadType PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
namespace ProtoOA.CommonMessages
{
    public sealed partial class ProtoHeartbeatEvent : IMessageExt { }
    public sealed partial class ProtoErrorRes : IMessageExt { }
}