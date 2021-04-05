using System;
using System.Text;

namespace WinForms.Demo
{
    public class OpenApiMessagesPresentation
    {
        private static string ProtoMessageToString(ProtoMessage msg)
        {
            if (!msg.HasPayload)
                return "ERROR in ProtoMessage: Corrupted execution event, no payload found";
            var _str = "ProtoMessage{";
            switch ((ProtoPayloadType)msg.PayloadType)
            {
                case ProtoPayloadType.ProtoMessage:
                    var _msg = ProtoMessage.Parser.ParseFrom(msg.Payload);
                    _str += ProtoMessageToString(_msg);
                    break;

                case ProtoPayloadType.HeartbeatEvent:
                    var _hb = ProtoHeartbeatEvent.Parser.ParseFrom(msg.Payload);
                    _str += "Heartbeat";
                    break;

                case ProtoPayloadType.ErrorRes:
                    var _err = ProtoErrorRes.Parser.ParseFrom(msg.Payload);
                    _str += "ErrorResponse{errorCode:" + _err.ErrorCode + (_err.HasDescription ? ", description:" + _err.Description : "") + "}";
                    break;

                default:
                    _str += OpenApiMessageToString(msg);
                    break;
            }

            _str += (msg.HasClientMsgId ? ", clientMsgId:" + msg.ClientMsgId : "") + "}";

            return _str;
        }

        private static string OpenApiMessageToString(ProtoMessage msg)
        {
            switch ((ProtoOAPayloadType)msg.PayloadType)
            {
                case ProtoOAPayloadType.ProtoOaApplicationAuthReq:
                    var app_auth_req = ProtoOAApplicationAuthReq.Parser.ParseFrom(msg.Payload);
                    return "AppAuthRequest{clientId:" + app_auth_req.ClientId + ", clientSecret:" + app_auth_req.ClientSecret + "}";

                case ProtoOAPayloadType.ProtoOaApplicationAuthRes:
                    return "ApAuthResponse";

                case ProtoOAPayloadType.ProtoOaAccountAuthReq:
                    var acc_auth_req = ProtoOAAccountAuthReq.Parser.ParseFrom(msg.Payload);
                    return "AccAuthRequest{CtidTraderAccountId:" + acc_auth_req.CtidTraderAccountId + "}";

                case ProtoOAPayloadType.ProtoOaAccountAuthRes:
                    return "AccAuthResponse";

                case ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq:
                    return "GetAccountsByAccessTokenReq";

                case ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenRes:
                    var accounts_list = ProtoOAGetAccountListByAccessTokenRes.Parser.ParseFrom(msg.Payload);
                    var sbAccounts = new StringBuilder();
                    foreach (var account in accounts_list.CtidTraderAccount)
                    {
                        sbAccounts.Append("ID: " + account.CtidTraderAccountId + (account.IsLive ? " Status: Live" + Environment.NewLine : " Status: Demo " + Environment.NewLine));
                    }
                    return "GetAccountsByAccessTokenRes{" + sbAccounts.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaTraderReq:
                    return "PotoOATraderReq";

                case ProtoOAPayloadType.ProtoOaSymbolsListReq:
                    return "GetSymbolsList";

                case ProtoOAPayloadType.ProtoOaSymbolsListRes:
                    var symbols_list = ProtoOASymbolsListRes.Parser.ParseFrom(msg.Payload);
                    var sbSymbols = new StringBuilder();
                    foreach (var symbol in symbols_list.Symbol)
                    {
                        sbSymbols.Append("ID: " + symbol.SymbolId + Environment.NewLine);
                        sbSymbols.Append("Name: " + symbol.SymbolName + Environment.NewLine);
                    }
                    return "Symbols{" + sbSymbols.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaSymbolByIdReq:
                    return "GetSymbolsById";

                case ProtoOAPayloadType.ProtoOaSymbolByIdRes:
                    var symbol_by_id_list = ProtoOASymbolByIdRes.Parser.ParseFrom(msg.Payload);
                    var sbSymbolByID = new StringBuilder();
                    foreach (var symbol in symbol_by_id_list.Symbol)
                    {
                        sbSymbolByID.Append("ID: " + symbol.SymbolId + Environment.NewLine);
                    }
                    return "Symbols{" + sbSymbolByID.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaExecutionEvent:
                    return OpenApiExecEventsToString(msg);

                case ProtoOAPayloadType.ProtoOaDealListReq:
                    return "DealListRequest{}";

                case ProtoOAPayloadType.ProtoOaDealListRes:
                    var deal_list = ProtoOADealListRes.Parser.ParseFrom(msg.Payload);
                    var sbDeals = new StringBuilder();
                    foreach (var deal in deal_list.Deal)
                    {
                        sbDeals.Append("ID: " + deal.DealId + Environment.NewLine);
                        sbDeals.Append("Status: " + deal.DealStatus + Environment.NewLine);
                        sbDeals.Append("Volume: " + deal.Volume + Environment.NewLine);
                    }
                    return "DealList{" + sbDeals.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaReconcileReq:
                    return "ReconcileRequest{}";

                case ProtoOAPayloadType.ProtoOaReconcileRes:
                    var reconcile_response = ProtoOAReconcileRes.Parser.ParseFrom(msg.Payload);
                    var sbReconcile = new StringBuilder();
                    foreach (var order in reconcile_response.Order)
                    {
                        sbReconcile.Append("ID: " + order.OrderId + Environment.NewLine);
                        sbReconcile.Append("Status: " + order.OrderStatus + Environment.NewLine);
                        sbReconcile.Append("Volume: " + order.TradeData.Volume + Environment.NewLine);
                    }
                    foreach (var position in reconcile_response.Position)
                    {
                        sbReconcile.Append("ID: " + position.HasPositionId + Environment.NewLine);
                        sbReconcile.Append("Status: " + position.PositionStatus + Environment.NewLine);
                        sbReconcile.Append("Volume: " + position.TradeData.Volume + Environment.NewLine);
                    }
                    return "ReconcileList{" + sbReconcile.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaCashFlowHistoryListReq:
                    return "CashFlowHistoryRequest{}";

                case ProtoOAPayloadType.ProtoOaCashFlowHistoryListRes:
                    var cashflow_history = ProtoOACashFlowHistoryListRes.Parser.ParseFrom(msg.Payload);
                    var sbDCashflow = new StringBuilder();
                    foreach (var entry in cashflow_history.DepositWithdraw)
                    {
                        sbDCashflow.Append("ID: " + entry.BalanceHistoryId + Environment.NewLine);
                        sbDCashflow.Append("Type: " + entry.OperationType + Environment.NewLine);
                        sbDCashflow.Append("Delta: " + entry.Delta + Environment.NewLine);
                    }
                    return "CashFlowHistory{" + sbDCashflow.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaGetTrendbarsReq:
                    return "GetTrendbarsRequest{}";

                case ProtoOAPayloadType.ProtoOaGetTrendbarsRes:
                    var trendbar = ProtoOAGetTrendbarsRes.Parser.ParseFrom(msg.Payload);
                    var sbTrendbar = new StringBuilder();
                    foreach (var entry in trendbar.Trendbar)
                    {
                        sbTrendbar.Append("Open: " + entry.DeltaOpen + Environment.NewLine);
                        sbTrendbar.Append("High: " + entry.DeltaHigh + Environment.NewLine);
                        sbTrendbar.Append("Low: " + entry.Low + Environment.NewLine);
                        sbTrendbar.Append("Close: " + entry.DeltaClose + Environment.NewLine);
                        sbTrendbar.Append("Timestamp: " + entry.UtcTimestampInMinutes + Environment.NewLine);
                    }
                    return "Trendbars{" + sbTrendbar.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaGetTickdataReq:
                    return "GetTickDataRequest{}";

                case ProtoOAPayloadType.ProtoOaGetTickdataRes:
                    var tickData = ProtoOAGetTickDataRes.Parser.ParseFrom(msg.Payload);
                    var sbTickData = new StringBuilder();
                    foreach (var entry in tickData.TickData)
                    {
                        sbTickData.Append("Tick: " + entry.Tick + " " + msg.ClientMsgId + Environment.NewLine);
                        sbTickData.Append("Timestamp: " + entry.Timestamp + Environment.NewLine);
                    }
                    return "Tick Data{" + sbTickData.ToString() + "}";

                case ProtoOAPayloadType.ProtoOaCancelOrderReq:
                    return "CancelOrderRequest{}";

                case ProtoOAPayloadType.ProtoOaNewOrderReq:
                    return "CreateOrderRequest{}";

                case ProtoOAPayloadType.ProtoOaClosePositionReq:
                    return "ClosePositionRequest{}";

                case ProtoOAPayloadType.ProtoOaAmendOrderReq:
                    return "AmendOrderRequest{}";

                case ProtoOAPayloadType.ProtoOaAmendPositionSltpReq:
                    return "AmendPositionRequest{}";

                case ProtoOAPayloadType.ProtoOaSubscribeSpotsReq:
                    return "SubscribeForSpotsRequest{}";

                case ProtoOAPayloadType.ProtoOaSubscribeSpotsRes:
                    return "SubscribeForSpotsResponse{}";

                case ProtoOAPayloadType.ProtoOaUnsubscribeSpotsReq:
                    return "UnsubscribeFromSpotsRequest{}";

                case ProtoOAPayloadType.ProtoOaUnsubscribeSpotsRes:
                    return "UnsubscribeFromSpotsResponse{}";

                case ProtoOAPayloadType.ProtoOaSpotEvent:
                    var _spot_event = ProtoOASpotEvent.Parser.ParseFrom(msg.Payload);
                    return "SpotEvent{symbolId:" + _spot_event.SymbolId + ", bidPrice:" + (_spot_event.HasBid ? _spot_event.Bid.ToString() : "       ") + ", askPrice:" + (_spot_event.HasAsk ? _spot_event.Ask.ToString() : "       ") + "}";

                case ProtoOAPayloadType.ProtoOaErrorRes:
                    var _err = ProtoOAErrorRes.Parser.ParseFrom(msg.Payload);
                    return "ErrorResponse{errorCode:" + _err.ErrorCode + (_err.HasDescription ? ", description:" + _err.Description : "") + "}";

                case ProtoOAPayloadType.ProtoOaOrderErrorEvent:
                    var _orderErr = ProtoOAOrderErrorEvent.Parser.ParseFrom(msg.Payload);
                    return "OrderErrorResponse{errorCode:" + _orderErr.ErrorCode + (_orderErr.HasDescription ? ", description:" + _orderErr.Description : "") + "}";

                default:
                    return "unknown";
            }
        }

        private static string OpenApiExecutionTypeToString(ProtoOAExecutionType executionType)
        {
            switch (executionType)
            {
                case ProtoOAExecutionType.OrderAccepted:
                    return "OrderAccepted";

                case ProtoOAExecutionType.OrderReplaced:
                    return "OrderAmended";

                case ProtoOAExecutionType.OrderCancelRejected:
                    return "OrderCancelRejected";

                case ProtoOAExecutionType.OrderCancelled:
                    return "OrderCancelled";

                case ProtoOAExecutionType.OrderExpired:
                    return "OrderExpired";

                case ProtoOAExecutionType.OrderFilled:
                    return "OrderFilled";

                case ProtoOAExecutionType.OrderRejected:
                    return "OrderRejected";

                default:
                    return "unknown";
            }
        }

        private static string OpenApiExecEventsToString(ProtoMessage msg)
        {
            if ((ProtoOAPayloadType)msg.PayloadType != ProtoOAPayloadType.ProtoOaExecutionEvent)
                return "ERROR in OpenApiExecutionEvents: Wrong message type";

            if (!msg.HasPayload)
                return "ERROR in OpenApiExecutionEvents: Corrupted execution event, no payload found";

            var _msg = ProtoOAExecutionEvent.Parser.ParseFrom(msg.Payload);
            var _str = OpenApiExecutionTypeToString(_msg.ExecutionType) + "{" +
                OpenApiOrderToString(_msg.Order) +
                ", " + OpenApiPositionToString(_msg.Position) +
                (_msg.HasErrorCode ? ", errorCode:" + _msg.ErrorCode : "");

            return _str + "}";
        }

        static public string OpenApiOrderTypeToString(ProtoOAOrderType orderType)
        {
            switch (orderType)
            {
                case ProtoOAOrderType.Limit:
                    return "LIMIT";

                case ProtoOAOrderType.Market:
                    return "MARKET";

                case ProtoOAOrderType.MarketRange:
                    return "MARKET RANGE";

                case ProtoOAOrderType.Stop:
                    return "STOP";

                default:
                    return "unknown";
            }
        }

        static public string TradeSideToString(ProtoOATradeSide tradeSide)
        {
            switch (tradeSide)
            {
                case ProtoOATradeSide.Buy:
                    return "BUY";

                case ProtoOATradeSide.Sell:
                    return "SELL";

                default:
                    return "unknown";
            }
        }

        static public string OpenApiOrderToString(ProtoOAOrder order)
        {
            var _str = "Order{orderId:" + order.OrderId.ToString() + ", orderType:" + OpenApiOrderTypeToString(order.OrderType);
            _str += ", tradeSide:" + TradeSideToString(order.TradeData.TradeSide);
            _str += ", symbolName:" + order.TradeData.SymbolId + ", requestedVolume:" + order.TradeData.Volume.ToString() + ", executedVolume:" + order.ExecutedVolume.ToString() + ", closingOrder:" +
                (order.ClosingOrder ? "TRUE" : "FALSE") +
                (order.HasExecutionPrice ? ", executionPrice:" + order.ExecutionPrice.ToString() : "") +
                (order.HasLimitPrice ? ", limitPrice:" + order.LimitPrice.ToString() : "") +
                (order.HasStopPrice ? ", stopPrice:" + order.StopPrice.ToString() : "") +
                (order.HasStopLoss ? ", stopLossPrice:" + order.StopLoss.ToString() : "") +
                (order.HasTakeProfit ? ", takeProfitPrice:" + order.TakeProfit.ToString() : "") +
                (order.HasBaseSlippagePrice ? ", baseSlippagePrice:" + order.BaseSlippagePrice.ToString() : "") +
                (order.HasSlippageInPoints ? ", slippageInPoints:" + order.SlippageInPoints.ToString() : "") +
                (order.HasRelativeStopLoss ? ", relativeStopLoss:" + order.RelativeStopLoss.ToString() : "") +
                (order.HasRelativeTakeProfit ? ", relativeTakeProfit:" + order.RelativeTakeProfit.ToString() : "") +
                (order.HasExpirationTimestamp ? ", expirationTimestamp:" + order.ExpirationTimestamp.ToString() : "");
            return _str + "}";
        }

        static public string OpenApiPositionStatusToString(ProtoOAPositionStatus positionStatus)
        {
            switch (positionStatus)
            {
                case ProtoOAPositionStatus.PositionStatusClosed:
                    return "CLOSED";

                case ProtoOAPositionStatus.PositionStatusOpen:
                    return "OPENED";

                default:
                    return "unknown";
            }
        }

        static public string OpenApiPositionToString(ProtoOAPosition position)
        {
            var _str = "Position{positionId:" + position.PositionId.ToString() + ", positionStatus:" + OpenApiPositionStatusToString(position.PositionStatus);
            _str += ", tradeSide:" + TradeSideToString(position.TradeData.TradeSide);
            _str += ", symbolId:" + position.TradeData.SymbolId + ", volume:" + position.TradeData.Volume.ToString() + ", Price:" + position.Price.ToString() + ", swap:" + position.Swap.ToString() +
                ", commission:" + position.Commission.ToString() + ", openTimestamp:" + position.TradeData.OpenTimestamp.ToString() +
                (position.HasStopLoss ? ", stopLossPrice:" + position.StopLoss.ToString() : "") +
                (position.HasTakeProfit ? ", takeProfitPrice:" + position.TakeProfit.ToString() : "");

            return _str + "}";
        }

        static public string OpenApiClosePositionDetails(ProtoOAClosePositionDetail closePositionDetails)
        {
            return "ClosePositionDetails{entryPrice:" + closePositionDetails.EntryPrice.ToString() +
                ", profit:" + closePositionDetails.GrossProfit.ToString() +
                ", swap:" + closePositionDetails.Swap.ToString() +
                ", commission:" + closePositionDetails.Commission.ToString() +
                ", balance:" + closePositionDetails.Balance.ToString() +
                (closePositionDetails.HasQuoteToDepositConversionRate ? ", quoteToDepositConversionRate:" + closePositionDetails.QuoteToDepositConversionRate.ToString() : "") +
                ", closedVolume:" + closePositionDetails.ClosedVolume.ToString() +
                "}";
        }

        static public string ToString(ProtoMessage msg)
        {
            return ProtoMessageToString(msg);
        }
    }
}