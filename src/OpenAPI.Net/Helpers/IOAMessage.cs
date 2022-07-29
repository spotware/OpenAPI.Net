using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pb = global::Google.Protobuf;
using OpenAPI.Net.Helpers;
using Google.Protobuf;

namespace OpenAPI.Net.Helpers
{
    public interface IOAMessage : IMessage
    {
        public global::ProtoOAPayloadType PayloadType { get; set; }
        public bool HasPayloadType { get; }
    }
}
#region IOpenAPIMessageDeclarations
public sealed partial class ProtoOAApplicationAuthReq : IOAMessage { }
public sealed partial class ProtoOAApplicationAuthRes : IOAMessage { }
public sealed partial class ProtoOAAccountAuthReq : IOAMessage { }
public sealed partial class ProtoOAAccountAuthRes : IOAMessage { }
public sealed partial class ProtoOAErrorRes : IOAMessage { }
public sealed partial class ProtoOAClientDisconnectEvent : IOAMessage { }
public sealed partial class ProtoOAAccountsTokenInvalidatedEvent : IOAMessage { }
public sealed partial class ProtoOAVersionReq : IOAMessage { }
public sealed partial class ProtoOAVersionRes : IOAMessage { }
public sealed partial class ProtoOANewOrderReq : IOAMessage { }
public sealed partial class ProtoOAExecutionEvent : IOAMessage { }
public sealed partial class ProtoOACancelOrderReq : IOAMessage { }
public sealed partial class ProtoOAAmendOrderReq : IOAMessage { }
public sealed partial class ProtoOAAmendPositionSLTPReq : IOAMessage { }
public sealed partial class ProtoOAClosePositionReq : IOAMessage { }
public sealed partial class ProtoOATrailingSLChangedEvent : IOAMessage { }
public sealed partial class ProtoOAAssetListReq : IOAMessage { }
public sealed partial class ProtoOAAssetListRes : IOAMessage { }
public sealed partial class ProtoOASymbolsListReq : IOAMessage { }
public sealed partial class ProtoOASymbolsListRes : IOAMessage { }
public sealed partial class ProtoOASymbolByIdReq : IOAMessage { }
public sealed partial class ProtoOASymbolByIdRes : IOAMessage { }
public sealed partial class ProtoOASymbolsForConversionReq : IOAMessage { }
public sealed partial class ProtoOASymbolsForConversionRes : IOAMessage { }
public sealed partial class ProtoOASymbolChangedEvent : IOAMessage { }
public sealed partial class ProtoOAAssetClassListReq : IOAMessage { }
public sealed partial class ProtoOAAssetClassListRes : IOAMessage { }
public sealed partial class ProtoOATraderReq : IOAMessage { }
public sealed partial class ProtoOATraderRes : IOAMessage { }
public sealed partial class ProtoOATraderUpdatedEvent : IOAMessage { }
public sealed partial class ProtoOAReconcileReq : IOAMessage { }
public sealed partial class ProtoOAReconcileRes : IOAMessage { }
public sealed partial class ProtoOAOrderErrorEvent : IOAMessage { }
public sealed partial class ProtoOADealListReq : IOAMessage { }
public sealed partial class ProtoOADealListRes : IOAMessage { }
public sealed partial class ProtoOAOrderListReq : IOAMessage { }
public sealed partial class ProtoOAOrderListRes : IOAMessage { }
public sealed partial class ProtoOAExpectedMarginReq : IOAMessage { }
public sealed partial class ProtoOAExpectedMarginRes : IOAMessage { }
public sealed partial class ProtoOAMarginChangedEvent : IOAMessage { }
public sealed partial class ProtoOACashFlowHistoryListReq : IOAMessage { }
public sealed partial class ProtoOACashFlowHistoryListRes : IOAMessage { }
public sealed partial class ProtoOAGetAccountListByAccessTokenReq : IOAMessage { }
public sealed partial class ProtoOAGetAccountListByAccessTokenRes : IOAMessage { }
public sealed partial class ProtoOARefreshTokenReq : IOAMessage { }
public sealed partial class ProtoOARefreshTokenRes : IOAMessage { }
public sealed partial class ProtoOASubscribeSpotsReq : IOAMessage { }
public sealed partial class ProtoOASubscribeSpotsRes : IOAMessage { }
public sealed partial class ProtoOAUnsubscribeSpotsReq : IOAMessage { }
public sealed partial class ProtoOAUnsubscribeSpotsRes : IOAMessage { }
public sealed partial class ProtoOASpotEvent : IOAMessage { }
public sealed partial class ProtoOASubscribeLiveTrendbarReq : IOAMessage { }
public sealed partial class ProtoOASubscribeLiveTrendbarRes : IOAMessage { }
public sealed partial class ProtoOAUnsubscribeLiveTrendbarReq : IOAMessage { }
public sealed partial class ProtoOAUnsubscribeLiveTrendbarRes : IOAMessage { }
public sealed partial class ProtoOAGetTrendbarsReq : IOAMessage { }
public sealed partial class ProtoOAGetTrendbarsRes : IOAMessage { }
public sealed partial class ProtoOAGetTickDataReq : IOAMessage { }
public sealed partial class ProtoOAGetTickDataRes : IOAMessage { }
public sealed partial class ProtoOAGetCtidProfileByTokenReq : IOAMessage { }
public sealed partial class ProtoOAGetCtidProfileByTokenRes : IOAMessage { }
public sealed partial class ProtoOADepthEvent : IOAMessage { }
public sealed partial class ProtoOASubscribeDepthQuotesReq : IOAMessage { }
public sealed partial class ProtoOASubscribeDepthQuotesRes : IOAMessage { }
public sealed partial class ProtoOAUnsubscribeDepthQuotesReq : IOAMessage { }
public sealed partial class ProtoOAUnsubscribeDepthQuotesRes : IOAMessage { }
public sealed partial class ProtoOASymbolCategoryListReq : IOAMessage { }
public sealed partial class ProtoOASymbolCategoryListRes : IOAMessage { }
public sealed partial class ProtoOAAccountLogoutReq : IOAMessage { }
public sealed partial class ProtoOAAccountLogoutRes : IOAMessage { }
public sealed partial class ProtoOAAccountDisconnectEvent : IOAMessage { }
public sealed partial class ProtoOAMarginCallListReq : IOAMessage { }
public sealed partial class ProtoOAMarginCallListRes : IOAMessage { }
public sealed partial class ProtoOAMarginCallUpdateReq : IOAMessage { }
public sealed partial class ProtoOAMarginCallUpdateRes : IOAMessage { }
public sealed partial class ProtoOAMarginCallUpdateEvent : IOAMessage { }
public sealed partial class ProtoOAMarginCallTriggerEvent : IOAMessage { }
public sealed partial class ProtoOAGetDynamicLeverageByIDReq : IOAMessage { }
public sealed partial class ProtoOAGetDynamicLeverageByIDRes : IOAMessage { }
public sealed partial class ProtoOADealListByPositionIdReq : IOAMessage { }
public sealed partial class ProtoOADealListByPositionIdRes : IOAMessage { }
#endregion