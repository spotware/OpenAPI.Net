using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAPI.Net
{
    interface IOpenClient: IAsyncDisposable, IObservable<IMessage>
    {
    }
}
