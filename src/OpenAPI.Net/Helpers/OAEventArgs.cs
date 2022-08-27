using System;

namespace OpenAPI.Net.Helpers
{
    public class OAEventArgs<T> : EventArgs
    {
        public IEventMessage<T> Message { get; set; }
    }
}
