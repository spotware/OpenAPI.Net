using ProtoOA.Response;
using System;

namespace OpenAPI.Net.Exceptions
{
    public class ErrorResException : Exception
    {
        public ErrorRes Error;
        public override string Message { get => string.IsNullOrEmpty(Error.Description) ? Error.ErrorCode : $"{Error.ErrorCode}({Error.Description})"; }
        internal ErrorResException(ErrorRes res)
        {
            Error = new ErrorRes(res);
        }
    }
}
