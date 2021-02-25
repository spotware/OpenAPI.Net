using System;

namespace OpenAPI.Net.Exceptions
{
    public class WriteException : Exception
    {
        public WriteException(Exception innerException) : base("An exception occurred while writing on stream", innerException)
        {
        }
    }
}