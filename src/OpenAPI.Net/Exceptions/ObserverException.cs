using Google.Protobuf;
using System;

namespace OpenAPI.Net.Exceptions
{
    /// <summary>
    /// The exception that is thrown when calling an OpenClient observer OnNext method.
    /// The innerException is the real exception that was thrown.
    /// </summary>
    public class ObserverException : Exception
    {
        public ObserverException(Exception innerException, IObserver<IMessage> observer) :
            base("An exception occurred while calling an observer OnNext method", innerException)
        {
            Observer = observer;
        }

        public IObserver<IMessage> Observer { get; }
    }
}