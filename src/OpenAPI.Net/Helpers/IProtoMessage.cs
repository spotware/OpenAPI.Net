using Google.Protobuf;
using OpenAPI.Net.Helpers;

namespace OpenAPI.Net.Helpers
{
    public interface IProtoMessage : IMessage
    {
        uint PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
public sealed partial class ProtoMessage : IProtoMessage { }
