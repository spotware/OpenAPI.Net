using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAPI.Net.Exceptions
{
    public class ProtoOAOrderErrorEventException : Exception
    {
        public ProtoOAOrderErrorEvent ProtoError;
        public override string Message { get => string.IsNullOrEmpty(ProtoError.Description) ? ProtoError.ErrorCode : $"{ProtoError.ErrorCode}({ProtoError.Description})"; }
        public ProtoOAOrderErrorEventException(ProtoOAOrderErrorEvent res)
        {
            ProtoError = res;
        }
    }
}
