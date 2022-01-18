using System;

namespace OpenAPI.Net.Exceptions
{
    /// <summary>
    /// The exception that is thrown when calling OpenClient connect method.
    /// The innerException is the real exception that was thrown.
    /// </summary>
    public class ConnectionException : Exception
    {
        public ConnectionException(Exception innerException) : base("An exception occurred during OpenClient connection attempt", innerException)
        {
        }
    }
}