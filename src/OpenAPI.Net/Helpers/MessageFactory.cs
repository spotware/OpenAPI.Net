using Google.Protobuf;

namespace OpenAPI.Net.Helpers
{
    public static class MessageFactory
    {
        /// <summary>
        /// Returns a ProtoMessage based on your provided parameters (for ProtoPayloadType message types)
        /// </summary>
        /// <typeparam name="T">The payload message type</typeparam>
        /// <param name="message">The ProtoMessage message payload message</param>
        /// <param name="payloadType">The ProtoMessage message payload type</param>
        /// <param name="clientMessageId">The client message ID for ProtoMessage</param>
        /// <returns>ProtoMessage</returns>
        public static ProtoMessage GetMessage<T>(this T message, ProtoPayloadType payloadType, string clientMessageId = null)
            where T : IMessage
        {
            return GetMessage((uint)payloadType, message.ToByteString(), clientMessageId);
        }

        /// <summary>
        /// Returns a ProtoMessage based on your provided parameters (for ProtoOAPayloadType message types)
        /// </summary>
        /// <typeparam name="T">The payload message type</typeparam>
        /// <param name="message">The ProtoMessage message payload message</param>
        /// <param name="payloadType">The ProtoMessage message payload type</param>
        /// <param name="clientMessageId">The client message ID for ProtoMessage</param>
        /// <returns>ProtoMessage</returns>
        public static ProtoMessage GetMessage<T>(this T message, ProtoOAPayloadType payloadType,
            string clientMessageId = null) where T : IMessage
        {
            return GetMessage((uint)payloadType, message.ToByteString(), clientMessageId);
        }

        /// <summary>
        /// Returns the message type that is inside a response/event ProtoMessage Payload
        /// </summary>
        /// <param name="protoMessage">The ProtoMessage</param>
        /// <returns>IMessage</returns>
        public static IMessage GetMessage(ProtoMessage protoMessage)
        {
            var payload = protoMessage.Payload;
            
            return protoMessage.PayloadType switch
            {
                (int)ProtoPayloadType.ErrorRes => ProtoErrorRes.Parser.ParseFrom(payload),
                (int)ProtoPayloadType.HeartbeatEvent => ProtoHeartbeatEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaApplicationAuthReq => ProtoOAApplicationAuthReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaApplicationAuthRes => ProtoOAApplicationAuthRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAccountAuthReq => ProtoOAAccountAuthReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAccountAuthRes => ProtoOAAccountAuthRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaVersionReq => ProtoOAVersionReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaVersionRes => ProtoOAVersionRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaNewOrderReq => ProtoOANewOrderReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaTrailingSlChangedEvent => ProtoOATrailingSLChangedEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaCancelOrderReq => ProtoOACancelOrderReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAmendOrderReq => ProtoOAAmendOrderReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAmendPositionSltpReq => ProtoOAAmendPositionSLTPReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaClosePositionReq => ProtoOAClosePositionReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAssetListReq => ProtoOAAssetListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAssetListRes => ProtoOAAssetListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolsListReq => ProtoOASymbolsListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolsListRes => ProtoOASymbolsListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolByIdReq => ProtoOASymbolByIdReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolByIdRes => ProtoOASymbolByIdRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolsForConversionReq => ProtoOASymbolsForConversionReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolsForConversionRes => ProtoOASymbolsForConversionRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolChangedEvent => ProtoOASymbolChangedEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaTraderReq => ProtoOATraderReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaTraderRes => ProtoOATraderRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaTraderUpdateEvent => ProtoOATraderUpdatedEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaReconcileReq => ProtoOAReconcileReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaReconcileRes => ProtoOAReconcileRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaExecutionEvent => ProtoOAExecutionEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSubscribeSpotsReq => ProtoOASubscribeSpotsReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSubscribeSpotsRes => ProtoOASubscribeSpotsRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaUnsubscribeSpotsReq => ProtoOAUnsubscribeSpotsReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaUnsubscribeSpotsRes => ProtoOAUnsubscribeSpotsRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSpotEvent => ProtoOASpotEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderErrorEvent => ProtoOAOrderErrorEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDealListReq => ProtoOADealListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDealListRes => ProtoOADealListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSubscribeLiveTrendbarReq => ProtoOASubscribeLiveTrendbarReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaUnsubscribeLiveTrendbarReq => ProtoOAUnsubscribeLiveTrendbarReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetTrendbarsReq => ProtoOAGetTrendbarsReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetTrendbarsRes => ProtoOAGetTrendbarsRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaExpectedMarginReq => ProtoOAExpectedMarginReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaExpectedMarginRes => ProtoOAExpectedMarginRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginChangedEvent => ProtoOAMarginChangedEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaErrorRes => ProtoOAErrorRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaCashFlowHistoryListReq => ProtoOACashFlowHistoryListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaCashFlowHistoryListRes => ProtoOACashFlowHistoryListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetTickdataReq => ProtoOAGetTickDataReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetTickdataRes => ProtoOAGetTickDataRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAccountsTokenInvalidatedEvent => ProtoOAAccountsTokenInvalidatedEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaClientDisconnectEvent => ProtoOAClientDisconnectEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq => ProtoOAGetAccountListByAccessTokenReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenRes => ProtoOAGetAccountListByAccessTokenRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetCtidProfileByTokenReq => ProtoOAGetCtidProfileByTokenReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetCtidProfileByTokenRes => ProtoOAGetCtidProfileByTokenRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAssetClassListReq => ProtoOAAssetClassListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAssetClassListRes => ProtoOAAssetClassListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDepthEvent => ProtoOADepthEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSubscribeDepthQuotesReq => ProtoOASubscribeDepthQuotesReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSubscribeDepthQuotesRes => ProtoOASubscribeDepthQuotesRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaUnsubscribeDepthQuotesReq => ProtoOAUnsubscribeDepthQuotesReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaUnsubscribeDepthQuotesRes => ProtoOAUnsubscribeDepthQuotesRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolCategoryReq => ProtoOASymbolCategoryListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSymbolCategoryRes => ProtoOASymbolCategoryListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAccountLogoutReq => ProtoOAAccountLogoutReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAccountLogoutRes => ProtoOAAccountLogoutRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaAccountDisconnectEvent => ProtoOAAccountDisconnectEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaSubscribeLiveTrendbarRes => ProtoOASubscribeLiveTrendbarRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaUnsubscribeLiveTrendbarRes => ProtoOAUnsubscribeLiveTrendbarRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginCallListReq => ProtoOAMarginCallListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginCallListRes => ProtoOAMarginCallListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginCallUpdateReq => ProtoOAMarginCallUpdateReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginCallUpdateRes => ProtoOAMarginCallUpdateRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginCallUpdateEvent => ProtoOAMarginCallUpdateEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaMarginCallTriggerEvent => ProtoOAMarginCallTriggerEvent.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaRefreshTokenReq => ProtoOARefreshTokenReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaRefreshTokenRes => ProtoOARefreshTokenRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderListReq => ProtoOAOrderListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderListRes => ProtoOAOrderListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetDynamicLeverageReq => ProtoOAGetDynamicLeverageByIDReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetDynamicLeverageRes => ProtoOAGetDynamicLeverageByIDRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDealListByPositionIdReq => ProtoOADealListByPositionIdReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDealListByPositionIdRes => ProtoOADealListByPositionIdRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderDetailsReq => ProtoOAOrderDetailsReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderDetailsRes => ProtoOAOrderDetailsRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderListByPositionIdReq => ProtoOAOrderListByPositionIdReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaOrderListByPositionIdRes => ProtoOAOrderListByPositionIdRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDealOffsetListReq => ProtoOADealOffsetListReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaDealOffsetListRes => ProtoOADealOffsetListRes.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetPositionUnrealizedPnlReq => ProtoOAGetPositionUnrealizedPnLReq.Parser.ParseFrom(payload),
                (int)ProtoOAPayloadType.ProtoOaGetPositionUnrealizedPnlRes => ProtoOAGetPositionUnrealizedPnLRes.Parser.ParseFrom(payload),
                _ => null
            };
        }

        /// <summary>
        /// Returns a ProtoMessage based on your provided parameters
        /// </summary>
        /// <param name="payloadType">The message payloadType as unint</param>
        /// <param name="payload">The message payload as a ByteString</param>
        /// <param name="clientMessageId">The client message ID for ProtoMessage</param>
        /// <returns>ProtoMessage</returns>
        public static ProtoMessage GetMessage(uint payloadType, ByteString payload, string clientMessageId = null)
        {
            var message = new ProtoMessage
            {
                PayloadType = payloadType,
                Payload = payload,
            };

            if (!string.IsNullOrEmpty(clientMessageId))
            {
                message.ClientMsgId = clientMessageId;
            }

            return message;
        }
    }
}