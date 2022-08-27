using Google.Protobuf;
using ProtoOA.CommonMessages;
using ProtoOA.Enums;
using ProtoOA.Event;
using ProtoOA.Response;

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
        internal static ProtoMessage GetMessage<T>(this T message, ProtoPayloadType payloadType, string clientMessageId = null)
            where T : IMessage
        {
            return GetMessage((uint)payloadType, message.ToByteString(), clientMessageId);
        }

        /// <summary>
        /// Returns a ProtoMessage based on your provided parameters (for PayloadType message types)
        /// </summary>
        /// <typeparam name="T">The payload message type</typeparam>
        /// <param name="message">The ProtoMessage message payload message</param>
        /// <param name="payloadType">The ProtoMessage message payload type</param>
        /// <param name="clientMessageId">The client message ID for ProtoMessage</param>
        /// <returns>ProtoMessage</returns>
        internal static ProtoMessage GetMessage<T>(this T message, PayloadType payloadType,
            string clientMessageId = null) where T : IMessage
        {
            return GetMessage((uint)payloadType, message.ToByteString(), clientMessageId);
        }

        /// <summary>
        /// Returns the message type that is inside a response/event ProtoMessage Payload
        /// </summary>
        /// <param name="protoMessage">The ProtoMessage</param>
        /// <returns>IMessage</returns>
        internal static IMessage GetMessage(ProtoMessage protoMessage)
        {
            var payload = protoMessage.Payload;
            
            return protoMessage.PayloadType switch
            {
                (int)PayloadType.ProtoOaErrorRes => ErrorRes.Parser.ParseFrom(payload),
                (int)ProtoPayloadType.HeartbeatEvent => ProtoHeartbeatEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaAccountAuthRes => AccountAuthRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaApplicationAuthRes => ApplicationAuthRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaClientDisconnectEvent => ClientDisconnectEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaDealListRes => DealListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaAssetListRes => AssetListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaAssetClassListRes => AssetClassListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaAccountsTokenInvalidatedEvent => AccountsTokenInvalidatedEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaCashFlowHistoryListRes => CashFlowHistoryListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaExecutionEvent => ExecutionEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaExpectedMarginRes => ExpectedMarginRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaGetAccountsByAccessTokenRes => GetAccountListByAccessTokenRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaGetTickdataRes => GetTickDataRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaGetTrendbarsRes => GetTrendbarsRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaMarginChangedEvent => MarginChangedEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaOrderErrorEvent => OrderErrorEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaReconcileRes => ReconcileRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSpotEvent => SpotEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSubscribeSpotsRes => SubscribeSpotsRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSymbolsForConversionRes => SymbolsForConversionRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSymbolsListRes => SymbolsListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSymbolByIdRes => SymbolByIdRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSymbolChangedEvent => SymbolChangedEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaTraderRes => TraderRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaTraderUpdateEvent => TraderUpdatedEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaTrailingSlChangedEvent => TrailingSLChangedEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaUnsubscribeSpotsRes => UnsubscribeSpotsRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaVersionRes => VersionRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaGetCtidProfileByTokenRes => GetCtidProfileByTokenRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSymbolCategoryRes => SymbolCategoryListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaDepthEvent => DepthEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaSubscribeDepthQuotesRes => SubscribeDepthQuotesRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaUnsubscribeDepthQuotesRes => UnsubscribeDepthQuotesRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaAccountLogoutRes => AccountLogoutRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaRefreshTokenRes => RefreshTokenRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaAccountDisconnectEvent => AccountDisconnectEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaMarginCallListRes => MarginCallListRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaMarginCallUpdateRes => MarginCallUpdateRes.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaMarginCallUpdateEvent => MarginCallUpdateEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaMarginCallTriggerEvent => MarginCallTriggerEvent.Parser.ParseFrom(payload),
                (int)PayloadType.ProtoOaGetDynamicLeverageRes => GetDynamicLeverageByIDRes.Parser.ParseFrom(payload),
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
        internal static ProtoMessage GetMessage(uint payloadType, ByteString payload, string clientMessageId = null)
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