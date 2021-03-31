using MahApps.Metro.Controls.Dialogs;
using Prism.Services.Dialogs;
using System;
using System.Web;
using Trading.UI.Demo.Services;
using auth = OpenAPI.Net.Auth;

namespace Trading.UI.Demo.ViewModels
{
    public class AccountAuthViewModel : DialogAwareViewBase
    {
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IAppDispatcher _dispatcher;
        private string _browserAddress;
        private Action<string> _codeCallback;
        private bool _isCodeReceived;

        public AccountAuthViewModel(IDialogCoordinator dialogCordinator, IAppDispatcher dispatcher)
        {
            _dialogCordinator = dialogCordinator;
            _dispatcher = dispatcher;

            Title = "Account Authorization";
        }

        public string BrowserAddress
        {
            get => _browserAddress;
            set
            {
                if (SetProperty(ref _browserAddress, value)) OnBrowserAddressChanged();
            }
        }

        private async void OnBrowserAddressChanged()
        {
            if (BrowserAddress.Contains("error=", StringComparison.OrdinalIgnoreCase))
            {
                var error = HttpUtility.ParseQueryString(new Uri(BrowserAddress).Query).Get("error");

                await _dialogCordinator.ShowMessageAsync(this, "Error", $"Something went wrong, {error}");

                OnRequestClose(new DialogResult(ButtonResult.Cancel));
            }
            else if (BrowserAddress.Contains("code=", StringComparison.OrdinalIgnoreCase))
            {
                var code = HttpUtility.ParseQueryString(new Uri(BrowserAddress).Query).Get("code");

                if (string.IsNullOrWhiteSpace(code))
                {
                    await _dialogCordinator.ShowMessageAsync(this, "Error", $"Code is not in redirect URL");

                    OnRequestClose(new DialogResult(ButtonResult.Cancel));

                    return;
                }

                _isCodeReceived = true;

                _codeCallback?.Invoke(code);

                OnRequestClose(new DialogResult(ButtonResult.OK));
            }
        }

        public override void OnDialogClosed()
        {
            if (_isCodeReceived is false) _dispatcher.InvokeShutdown();

            BrowserAddress = string.Empty;

            _codeCallback = null;

            _isCodeReceived = false;
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            if (!parameters.TryGetValue("App", out auth.App app)) throw new ArgumentException("App is not inside parameters", nameof(parameters));

            if (parameters.TryGetValue("CodeCallback", out Action<string> codeCallback)) _codeCallback = codeCallback;

            BrowserAddress = app.GetAuthUri().ToString();
        }
    }
}