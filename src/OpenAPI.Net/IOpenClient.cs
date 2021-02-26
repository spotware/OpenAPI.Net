using Google.Protobuf;
using System;

namespace OpenAPI.Net
{
    internal interface IOpenClient : IDisposable, IObservable<IMessage>
    {
    }
}