using System;
using System.Windows.Threading;

namespace Trading.UI.Sample.Services
{
    public interface IAppDispatcher
    {
        void Invoke(Action callback);

        public DispatcherOperation InvokeAsync(Action callback);

        public void InvokeShutdown();
    }

    public sealed class AppDispatcher : IAppDispatcher
    {
        private readonly Dispatcher _dispatcher;

        public AppDispatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void Invoke(Action callback) => _dispatcher.Invoke(callback);

        public DispatcherOperation InvokeAsync(Action callback) => _dispatcher.InvokeAsync(callback);

        public void InvokeShutdown() => _dispatcher.InvokeShutdown();
    }
}