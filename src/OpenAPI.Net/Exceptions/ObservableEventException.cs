using Google.Protobuf;
using System;

namespace OpenAPI.Net.Exceptions
{
    /// <summary>
    /// The exception that is thrown when calling an ObservableEvent observer OnNext method.
    /// The innerException is the real exception that was thrown.
    /// </summary>
    public class ObservableEventException<TEventModel> : Exception
    {
        public ObservableEventException(Exception innerException, IObserver<TEventModel> observer) :
            base("An exception occurred while calling an observer OnNext method", innerException)
        {
            Observer = observer;
        }

        public IObserver<TEventModel> Observer { get; }
    }
}