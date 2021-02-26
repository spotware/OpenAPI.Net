using Google.Protobuf;
using System;

namespace OpenAPI.Net.Exceptions
{
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