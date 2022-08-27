using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;
using OpenAPI.Net.Helpers;
using Google.Protobuf;
using ProtoOA.Enums;

namespace OpenAPI.Net.Helpers
{
    public interface IOAMessage : IMessage
    {
        public PayloadType PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
#region IOAMessageDeclarations
namespace ProtoOA.Request
{
    public sealed partial class ApplicationAuthReq : IOAMessage { }
    public sealed partial class AccountAuthReq : IOAMessage { }
    public sealed partial class VersionReq : IOAMessage { }
    public sealed partial class NewOrderReq : IOAMessage { }
    public sealed partial class CancelOrderReq : IOAMessage { }
    public sealed partial class AmendOrderReq : IOAMessage { }
    public sealed partial class AmendPositionSLTPReq : IOAMessage { }
    public sealed partial class ClosePositionReq : IOAMessage { }
    public sealed partial class AssetListReq : IOAMessage { }
    public sealed partial class SymbolsListReq : IOAMessage { }
    public sealed partial class SymbolByIdReq : IOAMessage { }
    public sealed partial class SymbolsForConversionReq : IOAMessage { }
    public sealed partial class AssetClassListReq : IOAMessage { }
    public sealed partial class TraderReq : IOAMessage { }
    public sealed partial class ReconcileReq : IOAMessage { }
    public sealed partial class DealListReq : IOAMessage { }
    public sealed partial class OrderListReq : IOAMessage { }
    public sealed partial class ExpectedMarginReq : IOAMessage { }
    public sealed partial class CashFlowHistoryListReq : IOAMessage { }
    public sealed partial class GetAccountListByAccessTokenReq : IOAMessage { }
    public sealed partial class RefreshTokenReq : IOAMessage { }
    public sealed partial class SubscribeSpotsReq : IOAMessage { }
    public sealed partial class UnsubscribeSpotsReq : IOAMessage { }
    public sealed partial class SubscribeLiveTrendbarReq : IOAMessage { }
    public sealed partial class UnsubscribeLiveTrendbarReq : IOAMessage { }
    public sealed partial class GetTrendbarsReq : IOAMessage { }
    public sealed partial class GetTickDataReq : IOAMessage { }
    public sealed partial class GetCtidProfileByTokenReq : IOAMessage { }
    public sealed partial class SubscribeDepthQuotesReq : IOAMessage { }
    public sealed partial class UnsubscribeDepthQuotesReq : IOAMessage { }
    public sealed partial class SymbolCategoryListReq : IOAMessage { }
    public sealed partial class AccountLogoutReq : IOAMessage { }
    public sealed partial class MarginCallListReq : IOAMessage { }
    public sealed partial class MarginCallUpdateReq : IOAMessage { }
    public sealed partial class GetDynamicLeverageByIDReq : IOAMessage { }
    public sealed partial class DealListByPositionIdReq : IOAMessage { }
}
namespace ProtoOA.Response
{
    public sealed partial class ApplicationAuthRes : IOAMessage { }
    public sealed partial class AccountAuthRes : IOAMessage { }
    public sealed partial class ErrorRes : IOAMessage { }
    public sealed partial class VersionRes : IOAMessage { }
    public sealed partial class AssetListRes : IOAMessage { }
    public sealed partial class SymbolsListRes : IOAMessage { }
    public sealed partial class SymbolByIdRes : IOAMessage { }
    public sealed partial class SymbolsForConversionRes : IOAMessage { }
    public sealed partial class AssetClassListRes : IOAMessage { }
    public sealed partial class TraderRes : IOAMessage { }
    public sealed partial class ReconcileRes : IOAMessage { }
    public sealed partial class DealListRes : IOAMessage { }
    public sealed partial class OrderListRes : IOAMessage { }
    public sealed partial class ExpectedMarginRes : IOAMessage { }
    public sealed partial class CashFlowHistoryListRes : IOAMessage { }
    public sealed partial class GetAccountListByAccessTokenRes : IOAMessage { }
    public sealed partial class RefreshTokenRes : IOAMessage { }
    public sealed partial class SubscribeSpotsRes : IOAMessage { }
    public sealed partial class UnsubscribeSpotsRes : IOAMessage { }
    public sealed partial class SubscribeLiveTrendbarRes : IOAMessage { }
    public sealed partial class UnsubscribeLiveTrendbarRes : IOAMessage { }
    public sealed partial class GetTrendbarsRes : IOAMessage { }
    public sealed partial class GetTickDataRes : IOAMessage { }
    public sealed partial class GetCtidProfileByTokenRes : IOAMessage { }
    public sealed partial class SubscribeDepthQuotesRes : IOAMessage { }
    public sealed partial class UnsubscribeDepthQuotesRes : IOAMessage { }
    public sealed partial class SymbolCategoryListRes : IOAMessage { }
    public sealed partial class AccountLogoutRes : IOAMessage { }
    public sealed partial class MarginCallListRes : IOAMessage { }
    public sealed partial class MarginCallUpdateRes : IOAMessage { }
    public sealed partial class GetDynamicLeverageByIDRes : IOAMessage { }
    public sealed partial class DealListByPositionIdRes : IOAMessage { }
}
namespace ProtoOA.Event
{
    public sealed partial class ClientDisconnectEvent : IOAMessage { }
    public sealed partial class AccountsTokenInvalidatedEvent : IOAMessage { }
    public sealed partial class ExecutionEvent : IOAMessage { }
    public sealed partial class TrailingSLChangedEvent : IOAMessage { }
    public sealed partial class SymbolChangedEvent : IOAMessage { }
    public sealed partial class TraderUpdatedEvent : IOAMessage { }
    public sealed partial class OrderErrorEvent : IOAMessage { }
    public sealed partial class MarginChangedEvent : IOAMessage { }
    public sealed partial class SpotEvent : IOAMessage { }
    public sealed partial class DepthEvent : IOAMessage { }
    public sealed partial class AccountDisconnectEvent : IOAMessage { }
    public sealed partial class MarginCallUpdateEvent : IOAMessage { }
    public sealed partial class MarginCallTriggerEvent : IOAMessage { }
}
#endregion