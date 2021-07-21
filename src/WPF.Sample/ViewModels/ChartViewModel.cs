using MahApps.Metro.Controls.Dialogs;
using OpenAPI.Net.Helpers;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Trading.UI.Sample.Events;
using Trading.UI.Sample.Models;
using Trading.UI.Sample.Services;

namespace Trading.UI.Sample.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IApiService _apiService;
        private SymbolModel _selectedSymbol;

        private AccountModel _account;
        private ProtoOATrendbarPeriod _selectedTimeFrame = ProtoOATrendbarPeriod.H1;
        private bool _isLoadingData;

        public ChartViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IDialogCoordinator dialogCordinator, IApiService apiService, IChartingService chartingService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;
            Symbols = new ObservableCollection<SymbolModel>();

            Chart = chartingService.GetChart();
        }

        public IChart Chart { get; }

        public ObservableCollection<SymbolModel> Symbols { get; }

        public bool IsLoadingData { get => _isLoadingData; set => SetProperty(ref _isLoadingData, value); }

        public SymbolModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                var oldSymbol = _selectedSymbol;

                if (SetProperty(ref _selectedSymbol, value)) OnSymbolChanged(oldSymbol, _selectedSymbol);
            }
        }

        public ProtoOATrendbarPeriod SelectedTimeFrame
        {
            get => _selectedTimeFrame; set
            {
                if (SetProperty(ref _selectedTimeFrame, value)) OnTimeFrameChanged();
            }
        }

        protected override void Loaded()
        {
            _eventAggregator.GetEvent<AccountChangedEvent>().Subscribe(OnAccountChanged);
        }

        protected override void Unloaded()
        {
            if (SelectedSymbol is not null) SelectedSymbol.Tick -= SelectedSymbol_Tick;

            SelectedSymbol = null;

            Symbols.Clear();

            _account = null;
        }

        private void OnAccountChanged(AccountModel account)
        {
            SelectedSymbol = null;

            Symbols.Clear();

            if (account is null) return;

            _account = account;

            Symbols.AddRange(account.Symbols.OrderBy(iSymbol => iSymbol.Name));

            SelectedSymbol = Symbols.FirstOrDefault();
        }

        private async void OnSymbolChanged(SymbolModel oldSymbol, SymbolModel newSymbol)
        {
            if (oldSymbol is not null) oldSymbol.Tick -= SelectedSymbol_Tick;

            if (newSymbol is null) return;

            newSymbol.Tick += SelectedSymbol_Tick;

            await LoadSymbolDataOnChart();
        }

        private async void OnTimeFrameChanged() => await LoadSymbolDataOnChart();

        private void SelectedSymbol_Tick(SymbolModel obj)
        {
            if (obj != SelectedSymbol) return;
        }

        private async Task LoadSymbolDataOnChart()
        {
            IsLoadingData = true;

            try
            {
                var trendbars = await _apiService.GetTrendbars(_account.Id, _account.IsLive, default, DateTimeOffset.Now, SelectedTimeFrame, SelectedSymbol.Id);

                var data = new List<Ohlc>();

                foreach (var trendbar in trendbars)
                {
                    data.Add(new Ohlc
                    {
                        Low = SelectedSymbol.Data.GetPriceFromRelative(trendbar.Low),
                        High = SelectedSymbol.Data.GetPriceFromRelative(trendbar.Low + (long)trendbar.DeltaHigh),
                        Open = SelectedSymbol.Data.GetPriceFromRelative(trendbar.Low + (long)trendbar.DeltaOpen),
                        Close = SelectedSymbol.Data.GetPriceFromRelative(trendbar.Low + (long)trendbar.DeltaClose),
                        Time = DateTimeOffset.FromUnixTimeSeconds(trendbar.UtcTimestampInMinutes * 60)
                    });
                }

                Chart.LoadData(SelectedSymbol.Name, data);
            }
            finally
            {
                IsLoadingData = false;
            }
        }
    }
}