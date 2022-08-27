using ProtoOA.Event;
using System;

namespace OpenAPI.Net.Exceptions
{
    public class OrderErrorEventException : Exception
    {
        public OrderErrorEvent OrderError;
        public override string Message { get => string.IsNullOrEmpty(OrderError.Description) ? OrderError.ErrorCode : $"{OrderError.ErrorCode}({OrderError.Description})"; }
        internal OrderErrorEventException(OrderErrorEvent res)
        {
            OrderError = new OrderErrorEvent(res);
        }
    }
}
