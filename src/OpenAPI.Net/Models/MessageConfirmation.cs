using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAPI.Net.Models
{
    public struct MessageConfirmation
    {
        public ulong ClientMessageID;
        public bool RequestTimedOut;
        public Task SendMessageTask;

        public MessageConfirmation(ulong clientMessageId, bool requestTimedOut, Task sendMessageTask)
        {
            ClientMessageID = clientMessageId;
            RequestTimedOut = requestTimedOut;
            SendMessageTask = sendMessageTask;
        }
    }
}
