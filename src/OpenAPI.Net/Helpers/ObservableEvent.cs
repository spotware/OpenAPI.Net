using OpenAPI.Net.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;

namespace OpenAPI.Net.Helpers
{
    public class ObservableEvent<TEventModel> : IDisposable, IObservable<TEventModel> where TEventModel : IEventMessage<TEventModel>
    {
        private readonly ConcurrentDictionary<int, IObserver<TEventModel>> observers = new();
        /// <summary>
        /// If client is disposed then this will return True, otherwise False
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// If client stream is completed without any error then it will return True, otherwise False
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// If there was any error (exception) on client stream then this will return True, otherwise False
        /// </summary>
        public bool IsTerminated { get; private set; }
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            if (!IsTerminated) OnCompleted();
        }

        public IDisposable Subscribe(IObserver<TEventModel> observer)
        {
            ThrowObjectDisposedExceptionIfDisposed();

            var observerHashCode = observer.GetHashCode();

            _ = observers.AddOrUpdate(observerHashCode, observer, (key, oldObserver) => observer);

            return Disposable.Create(() => OnObserverDispose(observerHashCode));
        }
        /// <summary>
        /// Calls each observer OnNext with the message
        /// </summary>
        /// <param name="protoMessage">Message</param>
        internal void OnNext(TEventModel message)
        {
            if (message == null)
                return;
            foreach (var observer in observers.Values)
            {
                try
                {
                    TEventModel messageCopy = message.Clone();
                    observer.OnNext(messageCopy);
                }
                catch (Exception ex)
                {
                    var observerException = new ObservableEventException<TEventModel>(ex, observer);
                    try
                    {// Inform only current observer: User code error
                        observer.OnError(observerException);
                    }
                    catch (Exception ex2) when (ex == observerException)
                    {
                    }
                }
            }
        }
        /// <summary>
        /// Calls each observer OnError after an client exception thrown
        /// </summary>
        /// <param name="exception">Exception</param>
        internal void OnClientError(Exception exception)
        {
            if (IsTerminated) return;

            IsTerminated = true;

            Dispose();

            foreach (var observer in observers.Values)
            {
                try
                {
                    observer.OnError(exception);
                }
                catch (Exception ex) when (ex == exception)
                {
                }
            }

            observers.Clear();
        }
        /// <summary>
        /// Completes each observer by calling their OnCompleted method
        /// </summary>
        private void OnCompleted()
        {
            IsCompleted = true;

            foreach (var observer in observers.Values)
            {
                observer.OnCompleted();
            }
        }
        /// <summary>
        /// Removes the disposed observer from event observers collection
        /// </summary>
        /// <param name="observerKey">The observer hash code key</param>
        private void OnObserverDispose(int observerKey)
        {
            _ = observers.TryRemove(observerKey, out _);
        }
        /// <summary>
        /// Throws ObjectDisposedException if the client was disposed
        /// </summary>
        private void ThrowObjectDisposedExceptionIfDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
