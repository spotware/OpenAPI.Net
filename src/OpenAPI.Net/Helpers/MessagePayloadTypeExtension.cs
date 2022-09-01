using Google.Protobuf;
using System;
using System.Reflection;

namespace OpenAPI.Net.Helpers
{
    public static class MessagePayloadTypeExtension
    {
        /// <summary>
        /// This method returns the payload type of a message
        /// </summary>
        /// <typeparam name="T">IMessage</typeparam>
        /// <param name="message">The message</param>
        /// <returns>uint (Payload Type)</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static uint GetPayloadType<T>(this T message) where T : IMessage
        {
            switch (message)
            {
                case IOAMessage m1:
                    return (uint)m1.PayloadType;
                case ICommonMessage m2:
                    return (uint)m2.PayloadType;
                case IProtoMessage m3:
                    return m3.PayloadType;
                default:
                    PropertyInfo property;
                    try
                    {
                        property = message.GetType().GetProperty("PayloadType");
                    }
                    catch (Exception ex) when (ex is AmbiguousMatchException || ex is ArgumentNullException)
                    {
                        throw new InvalidOperationException($"Couldn't get the PayloadType of the message {message}", ex);
                    }
                    return Convert.ToUInt32(property.GetValue(message));
            }
        }
    }
}
