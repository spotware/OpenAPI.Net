using System;

namespace OpenAPI.Net.Exceptions
{
    /// <summary>
    /// The exception that is thrown while writing on network stream in OpenClient
    /// The innerException is the real exception that was thrown.
    /// </summary>
    public class SendException : Exception
    {
        public SendException(Exception innerException) : base("An exception occurred while writing on stream", innerException)
        {
        }
    }
}