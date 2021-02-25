using System;

namespace OpenAPI.Net.Exceptions
{
    public class ReadException : Exception
    {
        public ReadException(Exception innerException) : base("An exception occurred while reading from stream", innerException)
        {
        }
    }
}