using Google.Protobuf;
using OpenAPI.Net.Helpers;
using ProtoOA.Event;
using System;
using System.Linq;
using System.Threading.Tasks;
using static OpenAPI.Net.OpenClient;

namespace OpenAPI.Net
{
    public sealed partial class OpenClient
    {
        public delegate Task EventHandlerAsync<T>(object sender, OAEventArgs<T> eventArgs);

        private async Task Raise<T>(EventHandlerAsync<T> handlers, IEventMessage<T> message)
        {
            if (handlers == null | message == null)
                return;
            //Delegate[] subscribers = handlers.GetInvocationList();
            //foreach (var subscriber in subscribers)
            //    await ((EventHandlerAsync<T>)subscriber).Invoke(this, new OAEventArgs<T>() { Message = (IEventMessage<T>)message.Clone() });
            //await Task.WhenAll(subscribers.Select(s => ((EventHandlerAsync<T>)s).Invoke(this, new OAEventArgs<T>() { Message = (IEventMessage<T>)message.Clone() })));
            lock (this)
            {
                Delegate[] subscribers = handlers.GetInvocationList();
                Parallel.ForEach(subscribers, s => ((EventHandlerAsync<T>)s).Invoke(this, new OAEventArgs<T>() { Message = (IEventMessage<T>)message.Clone() }));
            }
        }
    }
}