using Google.Protobuf;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using OpenAPI.Net.Helpers;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IAppDispatcher _dispatcher;
        private ApiConfigurationModel _apiConfiguration;
        private auth.Token _token;
        private ProtoOACtidTraderAccount _selectedAccount;
        private AccountModel _accountModel;
        private auth.App _app;

        public ShellViewModel(IRegionManager regionManager, IDialogService dialogService, IEventAggregator eventAggregator,
            IDialogCoordinator dialogCordinator, IApiService apiService, IAppDispatcher dispatcher)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;
            _dispatcher = dispatcher;

            ChangeThemeCommand = new DelegateCommand(ChangeTheme);
        }

        public DelegateCommand ChangeThemeCommand { get; }

        public string Title { get; } = "Trading UI Demo";

        public ObservableCollection<ProtoOACtidTraderAccount> Accounts { get; } = new ObservableCollection<ProtoOACtidTraderAccount>();

        public ProtoOACtidTraderAccount SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value)) OnAccountChanged();
            }
        }

        public bool IsAccountSelected => SelectedAccount is not null;

        protected override void Loaded()
        {
            _regionManager.RequestNavigate(ShellViewRegions.ChartViewRegion, nameof(ChartView));
            _regionManager.RequestNavigate(ShellViewRegions.AccountDataViewRegion, nameof(AccountDataView));

            ShowApiConfigurationDialog();
        }

        private async void ShowApiConfigurationDialog()
        {
            _apiConfiguration = new ApiConfigurationModel();

            await _dispatcher.InvokeAsync(() => _dialogService.ShowDialog(nameof(ApiConfigurationView), new DialogParameters
            {
                {"Model", _apiConfiguration }
            }, ApiConfigurationDialogCallback));
        }

        private async void ApiConfigurationDialogCallback(IDialogResult dialogResult)
        {
            if (_apiConfiguration is null)
            {
                await ShowInvalidApiConfigurationDialog();

                return;
            }

            _app = new auth.App(_apiConfiguration.ClientId, _apiConfiguration.Secret, _apiConfiguration.RedirectUri);

            var progressDialogController = await _dialogCordinator.ShowProgressAsync(this, "Connecting", "Please wait...");

            try
            {
                await _apiService.Connect();

                SubscribeToErrors(_apiService.LiveObservable);
                SubscribeToErrors(_apiService.DemoObservable);

                await _apiService.AuthorizeApp(_app);

                if (progressDialogController.IsOpen) await progressDialogController.CloseAsync();

                if (_apiConfiguration.IsLoaded is false) await SaveApiConfiguration();

                await ShowAccountAuthDialog();
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
        }

        private async Task SaveApiConfiguration()
        {
            var dialogResult = await _dialogCordinator.ShowMessageAsync(this, "Save API Credentials", "Do you want to save your API credentials and load it next time?", MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings
                {
                    AffirmativeButtonText = "Yes, I wannt to save",
                    NegativeButtonText = "No"
                });

            if (dialogResult == MessageDialogResult.Negative) return;

            var saveFileDialog = new SaveFileDialog
            {
                DefaultExt = ".xml",
                Filter = "(.XML)|*.xml",
                CheckPathExists = true,
                ValidateNames = true,
                Title = "Save API Configuration"
            };

            if (saveFileDialog.ShowDialog() is false) return;

            _apiConfiguration.SaveToFile(saveFileDialog.FileName);
        }

        private async Task ShowAccountAuthDialog()
        {
            await _dispatcher.InvokeAsync(() => _dialogService.ShowDialog(nameof(AccountAuthView), new DialogParameters
                {
                    {"App", _app },
                    { "CodeCallback", new Action<string>(OnCodeReceived)}
                }, null));
        }

        private async void OnCodeReceived(string code)
        {
            _token = await auth.TokenFactory.GetToken(code, _app);

            await ShowAccounts();
        }

        private async Task ShowAccounts()
        {
            if (_token is null || string.IsNullOrWhiteSpace(_token.AccessToken))
            {
                await ShowInvalidApiConfigurationDialog();

                return;
            }

            var progressDialogController = await _dialogCordinator.ShowProgressAsync(this, "Getting Accounts", "Please wait...");

            ProtoOACtidTraderAccount[] accounts = null;

            try
            {
                accounts = await _apiService.GetAccountsList(_token.AccessToken);

                foreach (var account in accounts)
                {
                    await _apiService.AuthorizeAccount((long)account.CtidTraderAccountId, account.IsLive, _token.AccessToken);
                }
            }
            catch (TimeoutException)
            {
                await ShowInvalidApiConfigurationDialog();
            }
            finally
            {
                if (progressDialogController.IsOpen) await progressDialogController.CloseAsync();
            }

            Accounts.Clear();

            if (accounts is not null) Accounts.AddRange(accounts);

            await _dialogCordinator.ShowMessageAsync(this, "Accounts Loaded", "Please select one of your authorized accounts from accounts list in title bar");
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
                _dispatcher.InvokeShutdown();
            }
        }

        private void SubscribeToErrors(IObservable<IMessage> observable)
        {
            if (observable is null) throw new ArgumentNullException(nameof(observable));

            observable.ObserveOn(SynchronizationContext.Current).Subscribe(_ => { }, OnError);
            observable.OfType<ProtoErrorRes>().ObserveOn(SynchronizationContext.Current).Subscribe(OnErrorRes);
            observable.OfType<ProtoOAErrorRes>().ObserveOn(SynchronizationContext.Current).Subscribe(OnOaErrorRes);
            observable.OfType<ProtoOAOrderErrorEvent>().ObserveOn(SynchronizationContext.Current).Subscribe(OnOrderErrorRes);
            observable.OfType<ProtoOASpotEvent>().Subscribe(OnSpotEvent);
            observable.OfType<ProtoOAExecutionEvent>().ObserveOn(SynchronizationContext.Current).Subscribe(OnExecutionEvent);
        }

        private void OnExecutionEvent(ProtoOAExecutionEvent executionEvent)
        {
            var accountModel = _accountModel;

            if (accountModel is null || executionEvent.CtidTraderAccountId != accountModel.Id) return;

            var position = accountModel.Positions.FirstOrDefault(iPoisition => iPoisition.Id == executionEvent.Order.PositionId);
            var order = accountModel.PendingOrders.FirstOrDefault(iOrder => iOrder.Id == executionEvent.Order.OrderId);

            var symbol = accountModel.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == executionEvent.Order.TradeData.SymbolId);

            if (symbol is null) return;

            switch (executionEvent.ExecutionType)
            {
                case ProtoOAExecutionType.OrderFilled or ProtoOAExecutionType.OrderPartialFill:
                    if (position is not null)
                    {
                        position.Update(executionEvent.Position, position.Symbol);

                        if (position.Volume is 0) _accountModel.Positions.Remove(position);
                    }
                    else
                    {
                        accountModel.Positions.Add(new MarketOrderModel(executionEvent.Position, symbol));
                    }

                    if (order is not null) accountModel.PendingOrders.Remove(order);

                    break;

                case ProtoOAExecutionType.OrderCancelled:
                    if (order is not null) accountModel.PendingOrders.Remove(order);
                    if (position is not null && executionEvent.Order.OrderType == ProtoOAOrderType.StopLossTakeProfit) position.Update(executionEvent.Position, position.Symbol);
                    break;

                case ProtoOAExecutionType.OrderAccepted when (executionEvent.Order.OrderType == ProtoOAOrderType.StopLossTakeProfit):
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    if (order is not null) order.Update(executionEvent.Order, symbol);

                    break;

                case ProtoOAExecutionType.OrderAccepted when (executionEvent.Order.OrderStatus != ProtoOAOrderStatus.OrderStatusFilled
                    && executionEvent.Order.OrderType == ProtoOAOrderType.Limit
                    || executionEvent.Order.OrderType == ProtoOAOrderType.Stop
                    || executionEvent.Order.OrderType == ProtoOAOrderType.StopLimit):
                    accountModel.PendingOrders.Add(new PendingOrderModel(executionEvent.Order, symbol));

                    break;

                case ProtoOAExecutionType.OrderReplaced:
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    if (order is not null) order.Update(executionEvent.Order, symbol);
                    break;

                case ProtoOAExecutionType.Swap:
                    if (position is not null) position.Update(executionEvent.Position, position.Symbol);
                    break;
            }
        }

        private void OnSpotEvent(ProtoOASpotEvent spotEvent)
        {
            var accountModel = _accountModel;

            if (accountModel is null || spotEvent.CtidTraderAccountId != accountModel.Id) return;

            var symbol = accountModel.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == spotEvent.SymbolId);

            if (symbol is null) return;

            double bid = symbol.Bid;
            double ask = symbol.Ask;

            if (spotEvent.HasBid) bid = symbol.GetPriceFromRelative((long)spotEvent.Bid);
            if (spotEvent.HasAsk) ask = symbol.GetPriceFromRelative((long)spotEvent.Ask);

            symbol.OnTick(bid, ask);

            if (symbol.QuoteAsset.AssetId == accountModel.DepositAsset.AssetId && symbol.TickValue is 0)
            {
                symbol.TickValue = symbol.TickSize;
            }
            else
            {
                var conversionSymbol = accountModel.Symbols.FirstOrDefault(iSymbol => (iSymbol.BaseAsset.AssetId == symbol.QuoteAsset.AssetId
                    || iSymbol.QuoteAsset.AssetId == symbol.QuoteAsset.AssetId)
                    && (iSymbol.BaseAsset.AssetId == accountModel.DepositAsset.AssetId
                    || iSymbol.QuoteAsset.AssetId == accountModel.DepositAsset.AssetId));

                if (conversionSymbol is not null && conversionSymbol.Bid is not 0)
                {
                    symbol.TickValue = conversionSymbol.BaseAsset.AssetId == accountModel.DepositAsset.AssetId
                        ? symbol.TickSize / conversionSymbol.Bid
                        : symbol.TickSize * conversionSymbol.Bid;
                }
            }

            if (symbol.TickValue is not 0)
            {
                var positions = accountModel.Positions.ToArray();

                foreach (var position in positions)
                {
                    if (position.Symbol != symbol) continue;

                    position.OnSymbolTick();
                }

                _accountModel.UpdateStatus();
            }
        }

        private async void OnError(Exception exception)
        {
            await _dialogCordinator.ShowMessageAsync(this, $"Error {exception.GetType().Name}", exception.ToString());

            _dispatcher.InvokeShutdown();
        }

        private async void OnOrderErrorRes(ProtoOAOrderErrorEvent error)
        {
            await _dialogCordinator.ShowMessageAsync(this, $"Error {error.ErrorCode}", error.Description);
        }

        private async void OnOaErrorRes(ProtoOAErrorRes error)
        {
            await _dialogCordinator.ShowMessageAsync(this, $"Error {error.ErrorCode}", error.Description);
        }

        private async void OnErrorRes(ProtoErrorRes error)
        {
            await _dialogCordinator.ShowMessageAsync(this, $"Error {error.ErrorCode}", error.Description);
        }

        private async void OnAccountChanged()
        {
            RaisePropertyChanged(nameof(IsAccountSelected));

            var progressDialogController = await _dialogCordinator.ShowProgressAsync(this, "Changing Account", "Please wait...");

            try
            {
                if (_accountModel is not null)
                {
                    var oldAccountSymbolIds = _accountModel.Symbols.Select(iSymbol => iSymbol.Id).ToArray();

                    await _apiService.UnsubscribeFromSpots(_accountModel.Id, _accountModel.IsLive, oldAccountSymbolIds);
                }

                if (SelectedAccount is null) return;

                var accountId = (long)SelectedAccount.CtidTraderAccountId;
                var trader = await _apiService.GetTrader(accountId, SelectedAccount.IsLive);
                var assets = await _apiService.GetAssets(accountId, SelectedAccount.IsLive);
                var lightSymbols = await _apiService.GetLightSymbols(accountId, SelectedAccount.IsLive);

                _accountModel = new AccountModel
                {
                    Id = accountId,
                    IsLive = SelectedAccount.IsLive,
                    Symbols = await _apiService.GetSymbolModels(accountId, SelectedAccount.IsLive, lightSymbols, assets),
                    Trader = trader,
                    RegistrationTime = DateTimeOffset.FromUnixTimeMilliseconds(trader.RegistrationTimestamp),
                    Balance = MonetaryConverter.FromMonetary(trader.Balance),
                    Assets = new ReadOnlyCollection<ProtoOAAsset>(assets),
                    DepositAsset = assets.First(iAsset => iAsset.AssetId == trader.DepositAssetId)
                };

                await FillAccountOrders(_accountModel);

                var symbolIds = _accountModel.Symbols.Select(iSymbol => iSymbol.Id).ToArray();

                await _apiService.SubscribeToSpots(accountId, _accountModel.IsLive, symbolIds);

                _eventAggregator.GetEvent<AccountChangedEvent>().Publish(_accountModel);
            }
            finally
            {
                if (progressDialogController.IsOpen) await progressDialogController.CloseAsync();
            }
        }

        private async Task FillAccountOrders(AccountModel account)
        {
            var response = await _apiService.GetAccountOrders(account.Id, account.IsLive);

            var positions = from poisiton in response.Position
                            let positionSymbol = account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == poisiton.TradeData.SymbolId)
                            select new MarketOrderModel(poisiton, positionSymbol);

            var pendingOrders = from order in response.Order
                                where order.OrderType is ProtoOAOrderType.Limit or ProtoOAOrderType.Stop or ProtoOAOrderType.StopLimit
                                let orderSymbol = account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == order.TradeData.SymbolId)
                                select new PendingOrderModel(order, orderSymbol);

            await _dispatcher.InvokeAsync(() =>
            {
                account.Positions.AddRange(positions);
                account.PendingOrders.AddRange(pendingOrders);
            });
        }

        private void ChangeTheme() => _eventAggregator.GetEvent<ChangeThemeEvent>().Publish();
    }
}