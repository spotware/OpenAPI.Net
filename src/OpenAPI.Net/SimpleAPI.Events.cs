using Google.Protobuf;
using OpenAPI.Net.EventsArgs;
using System;
using System.Threading.Tasks;

namespace OpenAPI.Net
{
    public sealed partial class OpenClient
    {
        public delegate Task EventHandlerAsnyc<TEventArgs>(object sender, TEventArgs eventArgs);

        public event EventHandlerAsnyc<TrailingSlChangedEventArgs> TrailingSlChangedEvent;
        public event EventHandlerAsnyc<SymbolChangedEventArgs> SymbolChangedEvent;
        public event EventHandlerAsnyc<TraderUpdateEventArgs> TraderUpdateEvent;
        public event EventHandlerAsnyc<ExecutionEventArgs> ExecutionEvent;
        public event EventHandlerAsnyc<SpotEventArgs> SpotEvent;
        public event EventHandlerAsnyc<OrderErrorEventArgs> OrderErrorEvent;
        public event EventHandlerAsnyc<MarginChangedEventArgs> MarginChangedEvent;
        public event EventHandlerAsnyc<AccountsTokenInvalidatedEventArgs> AccountsTokenInvalidatedEvent;
        public event EventHandlerAsnyc<ClientDisconnectEventArgs> ClientDisconnectEvent;
        public event EventHandlerAsnyc<DepthEventArgs> DepthEvent;
        public event EventHandlerAsnyc<AccountDisconnectEventArgs> AccountDisconnectEvent;
        public event EventHandlerAsnyc<MarginCallUpdateEventArgs> MarginCallUpdateEvent;
        public event EventHandlerAsnyc<MarginCallTriggerEventArgs> MarginCallTriggerEvent;

        public async Task Raise<TEventArgs>(EventHandlerAsnyc<TEventArgs> handlers, TEventArgs eventArgs)
        {
            if (handlers == null)
                return;

            //foreach (var subscriber in handlers.GetInvocationList())
            //    await ((EventHandlerAsnyc<TEventArgs>)subscriber).Invoke(sender, eventArgs);
            //Delegate[] subscribers = handlers.GetInvocationList();
            //await Task.WhenAll(subscribers.Select(s => ((EventHandlerAsnyc<TEventArgs>)s).Invoke(this, eventArgs)));
            lock (this)
            {
                Delegate[] subscribers = handlers.GetInvocationList();
                Parallel.ForEach(subscribers, s => ((EventHandlerAsnyc<TEventArgs>)s).Invoke(this, eventArgs));
            }
        }

        private async Task MessageEventHandlersAsync(IMessage message)
        {

            switch (message)
            {
                case ProtoOASpotEvent pOASpotEvent:
                    await Raise(SpotEvent, new SpotEventArgs { Message = pOASpotEvent });
                    break;
                case ProtoOAExecutionEvent pOAExecutionEvent:
                    await Raise(ExecutionEvent, new ExecutionEventArgs { Message = pOAExecutionEvent });
                    break;
                case ProtoOATrailingSLChangedEvent pOATrailingSlChangedEvent:
                    await Raise(TrailingSlChangedEvent, new TrailingSlChangedEventArgs { Message = pOATrailingSlChangedEvent });
                    break;
                case ProtoOASymbolChangedEvent pOASymbolChangedEvent:
                    await Raise(SymbolChangedEvent, new SymbolChangedEventArgs { Message = pOASymbolChangedEvent });
                    break;
                case ProtoOATraderUpdatedEvent pOATraderUpdateEvent:
                    await Raise(TraderUpdateEvent, new TraderUpdateEventArgs { Message = pOATraderUpdateEvent });
                    break;
                case ProtoOAOrderErrorEvent pOAOrderErrorEvent:
                    await Raise(OrderErrorEvent, new OrderErrorEventArgs { Message = pOAOrderErrorEvent });
                    break;
                case ProtoOAMarginChangedEvent pOAMarginChangedEvent:
                    await Raise(MarginChangedEvent, new MarginChangedEventArgs { Message = pOAMarginChangedEvent });
                    break;
                case ProtoOAAccountsTokenInvalidatedEvent pOAAccountsTokenInvalidatedEvent:
                    await Raise(AccountsTokenInvalidatedEvent, new AccountsTokenInvalidatedEventArgs { Message = pOAAccountsTokenInvalidatedEvent });
                    break;
                case ProtoOAClientDisconnectEvent pOAClientDisconnectEvent:
                    await Raise(ClientDisconnectEvent, new ClientDisconnectEventArgs { Message = pOAClientDisconnectEvent });
                    break;
                case ProtoOADepthEvent pOADepthEvent:
                    await Raise(DepthEvent, new DepthEventArgs { Message = pOADepthEvent });
                    break;
                case ProtoOAAccountDisconnectEvent pOAAccountDisconnectEvent:
                    await Raise(AccountDisconnectEvent, new AccountDisconnectEventArgs { Message = pOAAccountDisconnectEvent });
                    break;
                case ProtoOAMarginCallUpdateEvent pOAMarginCallUpdateEvent:
                    await Raise(MarginCallUpdateEvent, new MarginCallUpdateEventArgs { Message = pOAMarginCallUpdateEvent });
                    break;
                case ProtoOAMarginCallTriggerEvent pOAMarginCallTriggerEvent:
                    await Raise(MarginCallTriggerEvent, new MarginCallTriggerEventArgs { Message = pOAMarginCallTriggerEvent });
                    break;

            }
        }
    }
}
