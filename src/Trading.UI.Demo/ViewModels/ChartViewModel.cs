using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using MahApps.Metro.Controls.Dialogs;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Trading.UI.Demo.Events;
using Trading.UI.Demo.Models;
using Trading.UI.Demo.Services;

namespace Trading.UI.Demo.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IApiService _apiService;

        private SymbolModel _selectedSymbol;

        private AccountModel _account;
        private ProtoOATrendbarPeriod _selectedPeriod = ProtoOATrendbarPeriod.H1;

        public ChartViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IDialogCoordinator dialogCordinator, IApiService apiService)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;

            Symbols = new ObservableCollection<SymbolModel>();

            ChartData = new SeriesCollection();

            TimeLabels = new ObservableCollection<string>();
        }

        public ObservableCollection<SymbolModel> Symbols { get; }

        public ObservableCollection<string> TimeLabels { get; }

        public SymbolModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                var oldSymbol = _selectedSymbol;

                if (SetProperty(ref _selectedSymbol, value)) OnSymbolChanged(oldSymbol, _selectedSymbol);
            }
        }

        public SeriesCollection ChartData { get; }

        public ProtoOATrendbarPeriod SelectedPeriod { get => _selectedPeriod; set => SetProperty(ref _selectedPeriod, value); }

        protected override void Loaded()
        {
            _eventAggregator.GetEvent<AccountChangedEvent>().Subscribe(OnAccountChanged);
        }

        protected override void Unloaded()
        {
            if (SelectedSymbol is not null) SelectedSymbol.Tick -= SelectedSymbol_Tick;

            SelectedSymbol = null;

            Symbols.Clear();

            ChartData.Clear();

            TimeLabels.Clear();

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

            ChartData.Clear();
            TimeLabels.Clear();

            if (newSymbol is null) return;

            newSymbol.Tick += SelectedSymbol_Tick;

            var trendbars = await _apiService.GetTrendbars(_account.Id, _account.IsLive, DateTimeOffset.Now.AddDays(-10), DateTimeOffset.Now, SelectedPeriod, newSymbol.Id);

            var ohlcSeries = new OhlcSeries { Values = new ChartValues<OhlcPoint>() };

            foreach (var trendbar in trendbars)
            {
                var low = trendbar.Low;
                var high = low + (long)trendbar.DeltaHigh;
                var open = low + (long)trendbar.DeltaOpen;
                var close = low + (long)trendbar.DeltaClose;

                var ohlcPoint = new OhlcPoint
                {
                    Low = newSymbol.GetPriceFromRelative(low),
                    High = newSymbol.GetPriceFromRelative(high),
                    Open = newSymbol.GetPriceFromRelative(open),
                    Close = newSymbol.GetPriceFromRelative(close)
                };

                ohlcSeries.Values.Add(ohlcPoint);

                TimeLabels.Add(DateTimeOffset.FromUnixTimeSeconds(trendbar.UtcTimestampInMinutes * 60).ToString("s"));
            }

            ChartData.Add(ohlcSeries);

            RaisePropertyChanged(nameof(TimeLabels));
        }

        private void SelectedSymbol_Tick(SymbolModel obj)
        {
        }
    }
}