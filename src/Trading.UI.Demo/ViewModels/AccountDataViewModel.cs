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

        private AccountModel _account;
        private IDisposable _reconcileSenderDisposable;
        private string _searchText;

        public AccountDataViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IDialogCoordinator dialogCordinator, IApiService apiService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;

            Positions = new ObservableCollection<PositionModel>();

            _positionsCollectionView = CollectionViewSource.GetDefaultView(Positions);

            _positionsCollectionView.Filter = FilterPositions;

            SelectedPositions = new ObservableCollection<PositionModel>();

            SelectedPositions.CollectionChanged += (sender, args) => RaisePropertyChanged(nameof(SelectedPositions));

            SelectedClosePositionTypes = new ObservableCollection<PositionCloseType>();

            CreateNewOrderCommand = new DelegateCommand(ShowCreateModifyOrderDialog);

            ClosePositionCommand = new DelegateCommand<PositionModel>(ClosePosition);

            ClosePositionsCommand = new DelegateCommand(ClosePositions);

            CloseSelectedPositionsCommand = new DelegateCommand(CloseSelectedPositions, () => SelectedPositions != null && SelectedPositions.Any()).ObservesProperty(() => SelectedPositions);

            ModifyPositionCommand = new DelegateCommand<PositionModel>(ModifyPosition);
        }

        public DelegateCommand CreateNewOrderCommand { get; }
        public DelegateCommand<PositionModel> ClosePositionCommand { get; }
        public DelegateCommand ClosePositionsCommand { get; }
        public DelegateCommand CloseSelectedPositionsCommand { get; }

        public DelegateCommand<PositionModel> ModifyPositionCommand { get; }

        public ObservableCollection<PositionModel> SelectedPositions { get; }
        public ObservableCollection<PositionModel> Positions { get; }

        public ObservableCollection<PositionCloseType> SelectedClosePositionTypes { get; }

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
            Positions.Clear();
            SelectedPositions.Clear();
            SelectedClosePositionTypes.Clear();

            _reconcileSenderDisposable?.Dispose();

            _account = null;
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
            _account = account;

            _reconcileSenderDisposable?.Dispose();

            _reconcileSenderDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).ObserveOn(SynchronizationContext.Current).Subscribe(async x =>
            {
                if (_account is null) return;

                var response = await _apiService.GetAccountOrders(_account.Id, _account.IsLive);

                UpdatePositions(response.Position);
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

                var positionSymbol = _account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == updatedPosition.TradeData.SymbolId);

                position.Update(updatedPosition, positionSymbol);
            }

            foreach (var position in positions)
            {
                if (currentPositions.Any(iPosition => iPosition.Id == position.PositionId)) continue;

                var positionSymbol = _account.Symbols.FirstOrDefault(iSymbol => iSymbol.Id == position.TradeData.SymbolId);

                Positions.Add(new PositionModel(position, positionSymbol));
            }
        }

        private bool FilterPositions(object obj)
        {
            if (string.IsNullOrEmpty(SearchText) || !(obj is PositionModel position)) return true;

            var comparison = StringComparison.OrdinalIgnoreCase;

            return _account.Symbols.First(symbol => symbol.Id == position.TradeData.SymbolId).Name
                       .IndexOf(SearchText, comparison) >= 0 ||
                   position.TradeData.Label.IndexOf(SearchText, comparison) >= 0 ||
                   position.TradeData.TradeSide.ToString().IndexOf(SearchText, comparison) >= 0;
        }

        private async Task RequestPositionsClose(PositionModel[] positions)
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

        private async void CloseSelectedPositions()
        {
            var positionsToClose = SelectedPositions.ToArray();

            var userConfirmation = await GetUserConfirmationForClosingPositions();

            if (!userConfirmation) return;

            await RequestPositionsClose(positionsToClose);
        }

        private async void ClosePosition(PositionModel position)
        {
            var userConfirmation = await GetUserConfirmationForClosingPositions();

            if (!userConfirmation) return;

            await RequestPositionsClose(new[] { position });
        }

        private async Task<bool> GetUserConfirmationForClosingPositions()
        {
            var result = await _dialogCordinator.ShowMessageAsync(this, "Confirm",
                "Are you sure about closing the selected position(s)?",
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

            var positionsToClose = new List<PositionModel>();

            var positionsCopy = Positions.ToList();

            foreach (var position in positionsCopy)
            {
                var shouldClose = DoesPositionMeetCloseTypes(position);

                if (shouldClose) positionsToClose.Add(position);
            }

            if (positionsToClose.Any())
                await RequestPositionsClose(positionsToClose.ToArray());
            else
                await _dialogCordinator.ShowMessageAsync(this, "Positions Not Found",
                    "Couldn't find any matching order to cancel");
        }

        private bool DoesPositionMeetCloseTypes(PositionModel position)
        {
            if (position.TradeData.TradeSide == ProtoOATradeSide.Buy &&
                SelectedClosePositionTypes.Contains(PositionCloseType.Buy) ||
                position.TradeData.TradeSide == ProtoOATradeSide.Sell &&
                SelectedClosePositionTypes.Contains(PositionCloseType.Sell))
                return true;

            return false;
        }

        private void ModifyPosition(PositionModel position)
        {
            _dialogService.ShowDialog(nameof(CreateModifyOrderView), new DialogParameters
            {
                { "Account", _account },
                { "Position", position }
            }, null);
        }
    }
}