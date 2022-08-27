using Google.Protobuf;
using OpenAPI.Net.Helpers;

namespace OpenAPI.Net.Helpers
{
    internal interface IProtoMessage : IMessage
    {
        uint PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
namespace ProtoOA.CommonMessages
{
    public sealed partial class ProtoMessage : IProtoMessage { }
}