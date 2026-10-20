using Google.Protobuf;
using OpenAPI.Net.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OpenAPI.Net.Tests.Helpers
{
    public class MessageFactoryTests
    {
        public static IEnumerable<object[]> OpenApiPayloadTypes() =>
            Enum.GetValues(typeof(ProtoOAPayloadType)).Cast<ProtoOAPayloadType>().Select(type => new object[] { (uint)type });

        public static IEnumerable<object[]> CommonPayloadTypes() =>
            Enum.GetValues(typeof(ProtoPayloadType)).Cast<ProtoPayloadType>()
                .Where(type => type != ProtoPayloadType.ProtoMessage)
                .Select(type => new object[] { (uint)type });

        [Theory]
        [MemberData(nameof(OpenApiPayloadTypes))]
        [MemberData(nameof(CommonPayloadTypes))]
        public void GetMessageReturnsMessageOfMatchingPayloadType(uint payloadType)
        {
            var protoMessage = new ProtoMessage { PayloadType = payloadType, Payload = ByteString.Empty };

            var message = MessageFactory.GetMessage(protoMessage);

            Assert.NotNull(message);

            var payloadTypeProperty = message.GetType().GetProperty("PayloadType");

            Assert.NotNull(payloadTypeProperty);
            Assert.Equal(payloadType, Convert.ToUInt32(payloadTypeProperty.GetValue(message)));
        }
    }
}
