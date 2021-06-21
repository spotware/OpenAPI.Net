using ASP.NET.Demo.Services;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Hubs
{
    public class ApiErrorsHub : Hub
    {
        private readonly IOpenApiService _openApiService;
        private readonly List<IDisposable> _disposables = new();
        private readonly Channel<Error> _errorsChannel = Channel.CreateUnbounded<Error>();

        public ApiErrorsHub(IOpenApiService openApiService)
        {
            _openApiService = openApiService;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _disposables.ForEach(disposable => disposable.Dispose());

            _disposables.Clear();

            _errorsChannel.Writer.TryComplete(exception);

            return base.OnDisconnectedAsync(exception);
        }

        private void SubscribeToErrors(IObservable<IMessage> observable)
        {
            _disposables.Add(observable.OfType<ProtoErrorRes>().Subscribe(OnErrorRes));
            _disposables.Add(observable.OfType<ProtoOAErrorRes>().Subscribe(OnOaErrorRes));
            _disposables.Add(observable.OfType<ProtoOAOrderErrorEvent>().Subscribe(OnOrderErrorRes));
        }

        private async void OnOrderErrorRes(ProtoOAOrderErrorEvent error)
        {
            await _errorsChannel.Writer.WriteAsync(new Error(error.Description, nameof(ProtoOAOrderErrorEvent)));
        }

        private async void OnOaErrorRes(ProtoOAErrorRes error)
        {
            await _errorsChannel.Writer.WriteAsync(new Error(error.Description, nameof(ProtoOAErrorRes)));
        }

        private async void OnErrorRes(ProtoErrorRes error)
        {
            await _errorsChannel.Writer.WriteAsync(new Error(error.Description, nameof(ProtoErrorRes)));
        }

        public async IAsyncEnumerable<Error> GetErrors([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            SubscribeToErrors(_openApiService.LiveObservable);
            SubscribeToErrors(_openApiService.DemoObservable);

            while (await _errorsChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_errorsChannel.Reader.TryRead(out var error))
                {
                    yield return error;
                }
            }
        }

        public record Error(string Message, string Type);
    }
}