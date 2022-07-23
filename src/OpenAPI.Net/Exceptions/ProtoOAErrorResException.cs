using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAPI.Net.Exceptions
{
    internal class ProtoOAErrorResException : Exception
    {
        public ProtoOAErrorRes ProtoError;
        public override string Message { get => string.IsNullOrEmpty(ProtoError.Description) ? ProtoError.ErrorCode : $"{ProtoError.ErrorCode}({ProtoError.Description})"; }
        public ProtoOAErrorResException(ProtoOAErrorRes res)
        {
            ProtoError = res;
        }
    }
}
