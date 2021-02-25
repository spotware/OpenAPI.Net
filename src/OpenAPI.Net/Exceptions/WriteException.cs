using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAPI.Net.Exceptions
{
    public class WriteException: Exception
    {
        public WriteException(Exception innerException): base("An exception occurred while writing on stream", innerException)
        {
        }
    }
}
