using System;
using System.Collections.Generic;
using System.Text;

namespace OpenAPI.Net.Exceptions
{
    public class ReadException: Exception
    {
        public ReadException(Exception innerException): base("An exception occurred while reading from stream", innerException)
        {
        }
    }
}
