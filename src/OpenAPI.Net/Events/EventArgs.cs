using ProtoOA.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAPI.Net.Helpers;

namespace OpenAPI.Net.EventsArgs
{
    public class OAEventArgs<T> : EventArgs
    {
        public IEventMessage<T> Message { get; set; }
    }
}
