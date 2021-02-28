using MahApps.Metro.Controls.Dialogs;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Trading.UI.Demo.Events;
using Trading.UI.Demo.Models;
using Trading.UI.Demo.Regions;
using Trading.UI.Demo.Services;
using Trading.UI.Demo.Views;
using auth = OpenAPI.Net.Auth;

namespace Trading.UI.Demo.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IApiService _apiService;
        private ApiConfigurationModel _apiConfiguration;
        private auth.Token _token;
        private ProtoOACtidTraderAccount _selectedAccount;

        public ShellViewModel(IRegionManager regionManager, IDialogService dialogService, IEventAggregator eventAggregator,
            IDialogCoordinator dialogCordinator, IApiService apiService)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;
        }

        public string Title { get; } = "Trading UI Demo";

        public ObservableCollection<ProtoOACtidTraderAccount> Accounts { get; } = new ObservableCollection<ProtoOACtidTraderAccount>();

        public ProtoOACtidTraderAccount SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                {
                    _eventAggregator.GetEvent<AccountChangedEvent>().Publish(value);
                }
            }
        }

        protected async override void Loaded()
        {
            _regionManager.RequestNavigate(ShellViewRegions.OrdersViewRegion, nameof(OrdersView));

            await _apiService.LiveClient.Connect();
            await _apiService.DemoClient.Connect();

            await Application.Current.Dispatcher.InvokeAsync(ShowApiConfigurationDialog);
        }

        private void ShowApiConfigurationDialog()
        {
            _apiConfiguration = new ApiConfigurationModel();

            _dialogService.ShowDialog(nameof(ApiConfigurationView), new DialogParameters
            {
                {"Model", _apiConfiguration }
            }, ApiConfigurationDialogCallback);
        }

        private async void ApiConfigurationDialogCallback(IDialogResult dialogResult)
        {
            if (_apiConfiguration is null)
            {
                await ShowInvalidApiConfigurationDialog();

                return;
            }

            var app = new auth.App(_apiConfiguration.ClientId, _apiConfiguration.Secret, _apiConfiguration.RedirectUri);

            var progressDialogController = await _dialogCordinator.ShowProgressAsync(this, "Connecting", "Please wait...");

            try
            {
                await _apiService.AuthorizeApp(app);
            }
            catch (TimeoutException)
            {
                await progressDialogController.CloseAsync();

                await ShowInvalidApiConfigurationDialog();
            }
            finally
            {
                if (progressDialogController.IsOpen) await progressDialogController.CloseAsync();
            }

            var authUri = app.GetAuthUri();

            System.Diagnostics.Process.Start("explorer.exe", $"\"{authUri}\"");

            var authCode = await _dialogCordinator.ShowInputAsync(this, "Authentication Code",
                "Please enter your authentication code, to get it continue the authentication on your opened web browser");

            _token = await auth.TokenFactory.GetToken(authCode, app);

            ShowAccounts();
        }

        private async void ShowAccounts()
        {
            if (_token is null || string.IsNullOrWhiteSpace(_token.AccessToken))
            {
                await ShowInvalidApiConfigurationDialog();

                return;
            }

            var accountListRequest = new ProtoOAGetAccountListByAccessTokenReq
            {
                AccessToken = _token.AccessToken
            };

            bool isResponseReceived = false;

            using var disposable = _apiService.LiveClient.OfType<ProtoOAGetAccountListByAccessTokenRes>()
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(async response =>
                {
                    isResponseReceived = true;

                    Accounts.Clear();

                    Accounts.AddRange(response.CtidTraderAccount);

                    await _dialogCordinator.ShowMessageAsync(this, "Accounts Loaded",
                        "Please select one of your authorized accounts from accounts list in title bar");
                });

            await _apiService.LiveClient.SendMessage(accountListRequest, ProtoOAPayloadType.ProtoOaGetAccountsByAccessTokenReq);

            var waitStartTime = DateTime.Now;

            while (!isResponseReceived && DateTime.Now - waitStartTime < TimeSpan.FromSeconds(5))
            {
                await Task.Delay(1000);
            }

            if (!isResponseReceived)
            {
                await ShowInvalidApiConfigurationDialog();
            }
        }

        private async Task ShowInvalidApiConfigurationDialog()
        {
            var result = await _dialogCordinator.ShowMessageAsync(this, "Error", "Invalid API Configuration",
                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings
                {
                    AffirmativeButtonText = "Retry",
                    NegativeButtonText = "Exit"
                });

            if (result == MessageDialogResult.Affirmative)
            {
                ShowApiConfigurationDialog();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}