using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Trading.UI.Demo.Enums;
using Trading.UI.Demo.Events;
using Trading.UI.Demo.Models;
using Trading.UI.Demo.Services;
using Trading.UI.Demo.Views;

namespace Trading.UI.Demo.ViewModels
{
    public class AccountDataViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IApiService _apiService;
        private readonly ICollectionView _positionsCollectionView;
        private readonly ICollectionView _pendingOrdersCollectionView;

        private AccountModel _account;
        private IDisposable _reconcileSenderDisposable;
        private string _searchText;

        public AccountDataViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IDialogCoordinator dialogCordinator, IApiService apiService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;

            Positions = new ObservableCollection<MarketOrderModel>();

            _positionsCollectionView = CollectionViewSource.GetDefaultView(Positions);

            _positionsCollectionView.Filter = FilterOrders;

            PendingOrders = new ObservableCollection<PendingOrderModel>();

            _pendingOrdersCollectionView = CollectionViewSource.GetDefaultView(PendingOrders);

            _pendingOrdersCollectionView.Filter = FilterOrders;

            SelectedPositions = new ObservableCollection<MarketOrderModel>();

            SelectedPositions.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(SelectedPositions));

            SelectedPendingOrders = new ObservableCollection<PendingOrderModel>();

            SelectedPendingOrders.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(SelectedPendingOrders));

            SelectedClosePositionTypes = new ObservableCollection<PositionCloseType>();

            SelectedCancelOrderTypes = new ObservableCollection<OrderCancelType>();

            CreateNewOrderCommand = new DelegateCommand(ShowCreateModifyOrderDialog);

            ClosePositionCommand = new DelegateCommand<MarketOrderModel>(ClosePosition);

            ClosePositionsCommand = new DelegateCommand(ClosePositions);

            CloseSelectedPositionsCommand = new DelegateCommand(CloseSelectedPositions, () => SelectedPositions != null && SelectedPositions.Any()).ObservesProperty(() => SelectedPositions);

            ModifyPositionCommand = new DelegateCommand<MarketOrderModel>(ModifyPosition);

            CancelOrderCommand = new DelegateCommand<PendingOrderModel>(CancelOrder);

            CancelOrdersCommand = new DelegateCommand(CancelOrders);

            CancelSelectedOrdersCommand = new DelegateCommand(CancelSelectedOrders, () => SelectedPendingOrders != null && SelectedPendingOrders.Any()).ObservesProperty(() => SelectedPendingOrders);

            ModifyOrderCommand = new DelegateCommand<PendingOrderModel>(ModifyOrder);
        }

        public DelegateCommand CreateNewOrderCommand { get; }
        public DelegateCommand<MarketOrderModel> ClosePositionCommand { get; }
        public DelegateCommand CancelOrdersCommand { get; }
        public DelegateCommand CancelSelectedOrdersCommand { get; }
        public DelegateCommand<PendingOrderModel> ModifyOrderCommand { get; }
        public DelegateCommand ClosePositionsCommand { get; }
        public DelegateCommand CloseSelectedPositionsCommand { get; }
        public DelegateCommand<MarketOrderModel> ModifyPositionCommand { get; }
        public DelegateCommand<PendingOrderModel> CancelOrderCommand { get; }

        public ObservableCollection<MarketOrderModel> SelectedPositions { get; }
        public ObservableCollection<MarketOrderModel> Positions { get; }
        public ObservableCollection<PendingOrderModel> SelectedPendingOrders { get; }
        public ObservableCollection<PendingOrderModel> PendingOrders { get; }
        public ObservableCollection<PositionCloseType> SelectedClosePositionTypes { get; }
        public ObservableCollection<OrderCancelType> SelectedCancelOrderTypes { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);

                _positionsCollectionView.Refresh();
            }
        }

        protected override void Loaded()
        {
            _eventAggregator.GetEvent<AccountChangedEvent>().Subscribe(OnAccountChanged);
        }

        protected override void Unloaded()
        {
            Cleanup();

            _account = null;
        }

        private void Cleanup()
        {
            _reconcileSenderDisposable?.Dispose();

            Positions.Clear();
            SelectedPositions.Clear();
            SelectedClosePositionTypes.Clear();
        }

        private async void ShowCreateModifyOrderDialog()
        {
            if (_account is null)
            {
                await _dialogCordinator.ShowMessageAsync(this, "Error", "Trading account is not selected yet");

                return;
            }

            _dialogService.ShowDialog(nameof(CreateModifyOrderView), new DialogParameters { { "Account", _account } }, null);
        }

        private void OnAccountChanged(AccountModel account)
        {
            Cleanup();

            _account = account;

            _reconcileSenderDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOn(SynchronizationContext.Current).Subscribe(async x =>
            {
                if (_account is null) return;

                var response = await _apiService.GetAccountOrders(_account.Id, _account.IsLive);

                UpdatePositions(response.Position);

                var pendingOrders = response.Order.Where(iOrder => iOrder.OrderType is ProtoOAOrderType.Limit or ProtoOAOrderType.Stop or ProtoOAOrderType.StopLimit);

                UpdatePendingOrders(pendingOrders);
            });
        }

        private void UpdatePositions(IEnumerable<ProtoOAPosition> positions)
        {
            var currentPositions = Positions.ToArray();

            foreach (var position in currentPositions)
            {
                var updatedPosition = positions.FirstOrDefault(iPosition => iPosition.PositionId == position.Id);

                if (updatedPosition is null)
                {
                    Positions.Remove(position);

                    continue;
                }

                var symbol = _account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == updatedPosition.TradeData.SymbolId);

                position.Update(updatedPosition, symbol);
            }

            foreach (var position in positions)
            {
                if (currentPositions.Any(iPosition => iPosition.Id == position.PositionId)) continue;

                var symbol = _account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == position.TradeData.SymbolId);

                Positions.Add(new MarketOrderModel(position, symbol));
            }
        }

        private void UpdatePendingOrders(IEnumerable<ProtoOAOrder> orders)
        {
            var currentOrders = PendingOrders.ToArray();

            foreach (var order in currentOrders)
            {
                var updatedOrder = orders.FirstOrDefault(iOrder => iOrder.OrderId == order.Id);

                if (updatedOrder is null)
                {
                    PendingOrders.Remove(order);

                    continue;
                }

                var symbol = _account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == updatedOrder.TradeData.SymbolId);

                order.Update(updatedOrder, symbol);
            }

            foreach (var order in orders)
            {
                if (currentOrders.Any(iOrder => iOrder.Id == order.OrderId)) continue;

                var symbol = _account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == order.TradeData.SymbolId);

                PendingOrders.Add(new PendingOrderModel(order, symbol));
            }
        }

        private bool FilterOrders(object obj)
        {
            if (string.IsNullOrEmpty(SearchText) || obj is not OrderModel order) return true;

            var comparison = StringComparison.OrdinalIgnoreCase;

            return _account.Symbols.First(symbol => symbol.Id == order.TradeData.SymbolId).Name.Contains(SearchText, comparison)
                || order.TradeData.Label.Contains(SearchText, comparison)
                || order.TradeData.TradeSide.ToString().Contains(SearchText, comparison);
        }

        private async Task SendPositionsCloseRequest(MarketOrderModel[] positions)
        {
            if (!positions.Any()) return;

            var progressStep = 1.0 / positions.Length;

            double currentProgress = 0;

            var dialogController = await _dialogCordinator.ShowProgressAsync(this, "Closing Position(s)", "Please wait...");

            try
            {
                foreach (var position in positions)
                {
                    await _apiService.ClosePosition(position.Id, position.TradeData.Volume, _account.Id, _account.IsLive);

                    currentProgress += progressStep;

                    dialogController.SetProgress(currentProgress);
                }
            }
            finally
            {
                if (dialogController.IsOpen) await dialogController.CloseAsync();
            }

            await _dialogCordinator.ShowMessageAsync(this, "Positions Close Result", "Closing request for position(s) have been sent");
        }

        private async Task SendOrdersCancelRequest(PendingOrderModel[] orders)
        {
            if (!orders.Any()) return;

            var progressStep = 1.0 / orders.Length;

            double currentProgress = 0;

            var dialogController = await _dialogCordinator.ShowProgressAsync(this, "Canceling Order(s)", "Please wait...");

            try
            {
                foreach (var order in orders)
                {
                    await _apiService.CancelOrder(order.Id, _account.Id, _account.IsLive);

                    currentProgress += progressStep;

                    dialogController.SetProgress(currentProgress);
                }
            }
            finally
            {
                if (dialogController.IsOpen) await dialogController.CloseAsync();
            }

            await _dialogCordinator.ShowMessageAsync(this, "Order(s) Cancel Result", "Canceling request for order(s) have been sent");
        }

        private async void CloseSelectedPositions()
        {
            var positionsToClose = SelectedPositions.ToArray();

            var userConfirmation = await GetUserConfirmationForClosingPositions();

            if (!userConfirmation) return;

            await SendPositionsCloseRequest(positionsToClose);
        }

        private async void ClosePosition(MarketOrderModel position)
        {
            var userConfirmation = await GetUserConfirmationForClosingPositions();

            if (!userConfirmation) return;

            await SendPositionsCloseRequest(new[] { position });
        }

        private async Task<bool> GetUserConfirmationForClosingPositions()
        {
            var result = await _dialogCordinator.ShowMessageAsync(this, "Confirm",
                "Are you sure about closing the selected position(s)?",
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });

            return result == MessageDialogResult.Affirmative;
        }

        private async Task<bool> GetUserConfirmationForCancelingOrders()
        {
            var result = await _dialogCordinator.ShowMessageAsync(this, "Confirm",
                "Are you sure about canceling the selected order(s)?",
                MessageDialogStyle.AffirmativeAndNegative,
                new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });

            return result == MessageDialogResult.Affirmative;
        }

        private async void ClosePositions()
        {
            if (!SelectedClosePositionTypes.Any())
            {
                await _dialogCordinator.ShowMessageAsync(this, "Error", "Please select position close type(s)");

                return;
            }

            var userConfirmation = await GetUserConfirmationForClosingPositions();

            if (!userConfirmation) return;

            var positionsToClose = new List<MarketOrderModel>();

            var positionsCopy = Positions.ToList();

            foreach (var position in positionsCopy)
            {
                var shouldClose = DoesPositionMatchCloseTypes(position);

                if (shouldClose) positionsToClose.Add(position);
            }

            if (positionsToClose.Any())
                await SendPositionsCloseRequest(positionsToClose.ToArray());
            else
                await _dialogCordinator.ShowMessageAsync(this, "Positions Not Found",
                    "Couldn't find any matching position to close");
        }

        private bool DoesPositionMatchCloseTypes(MarketOrderModel position)
        {
            if (position.TradeData.TradeSide == ProtoOATradeSide.Buy &&
                SelectedClosePositionTypes.Contains(PositionCloseType.Buy) ||
                position.TradeData.TradeSide == ProtoOATradeSide.Sell &&
                SelectedClosePositionTypes.Contains(PositionCloseType.Sell))
                return true;

            return false;
        }

        private void ModifyPosition(MarketOrderModel position)
        {
            _dialogService.ShowDialog(nameof(CreateModifyOrderView), new DialogParameters
            {
                { "Account", _account },
                { "Position", position }
            }, null);
        }

        private void ModifyOrder(PendingOrderModel order)
        {
            _dialogService.ShowDialog(nameof(CreateModifyOrderView), new DialogParameters
            {
                { "Account", _account },
                { "PendingOrder", order }
            }, null);
        }

        private async void CancelSelectedOrders()
        {
            var ordersToCancel = SelectedPendingOrders.ToArray();

            var userConfirmation = await GetUserConfirmationForCancelingOrders();

            if (!userConfirmation) return;

            await SendOrdersCancelRequest(ordersToCancel);
        }

        private async void CancelOrders()
        {
            if (!SelectedCancelOrderTypes.Any())
            {
                await _dialogCordinator.ShowMessageAsync(this, "Error", "Please select order cancel type(s)");

                return;
            }

            var userConfirmation = await GetUserConfirmationForCancelingOrders();

            if (!userConfirmation) return;

            var ordersToCancel = new List<PendingOrderModel>();

            var ordersCopy = PendingOrders.ToList();

            foreach (var order in ordersCopy)
            {
                if (DoesOrderMatchCancelTypes(order)) ordersToCancel.Add(order);
            }

            if (ordersToCancel.Any())
                await SendOrdersCancelRequest(ordersToCancel.ToArray());
            else
                await _dialogCordinator.ShowMessageAsync(this, "Orders Not Found",
                    "Couldn't find any matching order to cancel");
        }

        private bool DoesOrderMatchCancelTypes(PendingOrderModel order)
        {
            if (
                (order.TradeData.TradeSide == ProtoOATradeSide.Buy &&
                 SelectedCancelOrderTypes.Contains(OrderCancelType.Buy) ||
                 order.TradeData.TradeSide == ProtoOATradeSide.Sell &&
                 SelectedCancelOrderTypes.Contains(OrderCancelType.Sell) ||
                 !SelectedCancelOrderTypes.Contains(OrderCancelType.Buy) &&
                 !SelectedCancelOrderTypes.Contains(OrderCancelType.Sell))
                &&
                (order.Type == PendingOrderType.Limit && SelectedCancelOrderTypes.Contains(OrderCancelType.Limit) ||
                 order.Type == PendingOrderType.Stop && SelectedCancelOrderTypes.Contains(OrderCancelType.Stop) ||
                 order.Type == PendingOrderType.StopLimit &&
                 SelectedCancelOrderTypes.Contains(OrderCancelType.StopLimit) ||
                 !SelectedCancelOrderTypes.Contains(OrderCancelType.Limit) &&
                 !SelectedCancelOrderTypes.Contains(OrderCancelType.Stop) &&
                 !SelectedCancelOrderTypes.Contains(OrderCancelType.StopLimit)))
                return true;

            return false;
        }

        private async void CancelOrder(PendingOrderModel order)
        {
            var userConfirmation = await GetUserConfirmationForCancelingOrders();

            if (!userConfirmation) return;

            await SendOrdersCancelRequest(new[] { order });
        }
    }
}