using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAPI.Net.EventsArgs
{
    public class TrailingSlChangedEventArgs : EventArgs { public ProtoOATrailingSLChangedEvent Message { get; set; } }
    public class SymbolChangedEventArgs : EventArgs { public ProtoOASymbolChangedEvent Message { get; set; } }
    public class TraderUpdateEventArgs : EventArgs { public ProtoOATraderUpdatedEvent Message { get; set; } }
    public class ExecutionEventArgs : EventArgs { public ProtoOAExecutionEvent Message { get; set; } }
    public class SpotEventArgs : EventArgs { public ProtoOASpotEvent Message { get; set; } }
    public class OrderErrorEventArgs : EventArgs { public ProtoOAOrderErrorEvent Message { get; set; } }
    public class MarginChangedEventArgs : EventArgs { public ProtoOAMarginChangedEvent Message { get; set; } }
    public class AccountsTokenInvalidatedEventArgs : EventArgs { public ProtoOAAccountsTokenInvalidatedEvent Message { get; set; } }
    public class ClientDisconnectEventArgs : EventArgs { public ProtoOAClientDisconnectEvent Message { get; set; } }
    public class DepthEventArgs : EventArgs { public ProtoOADepthEvent Message { get; set; } }
    public class AccountDisconnectEventArgs : EventArgs { public ProtoOAAccountDisconnectEvent Message { get; set; } }
    public class MarginCallUpdateEventArgs : EventArgs { public ProtoOAMarginCallUpdateEvent Message { get; set; } }
    public class MarginCallTriggerEventArgs : EventArgs { public ProtoOAMarginCallTriggerEvent Message { get; set; } }
}
