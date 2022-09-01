using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;
using OpenAPI.Net.Helpers;
using Google.Protobuf;
using ProtoOA.Enums;

namespace OpenAPI.Net.Helpers
{
    public interface IOAMessage : IMessage
    {
        public PayloadType PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
