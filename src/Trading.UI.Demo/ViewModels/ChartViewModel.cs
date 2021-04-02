using MahApps.Metro.Controls.Dialogs;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private string _browserAddress;

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

        public string BrowserAddress { get => _browserAddress; set => SetProperty(ref _browserAddress, value); }

        public SymbolModel SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                var oldSymbol = _selectedSymbol;

                if (SetProperty(ref _selectedSymbol, value)) OnSymbolChanged(oldSymbol, _selectedSymbol);
            }
        }

        public ProtoOATrendbarPeriod SelectedPeriod { get => _selectedPeriod; set => SetProperty(ref _selectedPeriod, value); }

        protected override void Loaded()
        {
            _eventAggregator.GetEvent<AccountChangedEvent>().Subscribe(OnAccountChanged);

            BrowserAddress = Path.Combine(Environment.CurrentDirectory, "Chart.js", "chart.html");
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

            var trendbars = await _apiService.GetTrendbars(_account.Id, _account.IsLive, DateTimeOffset.Now.AddDays(-10), DateTimeOffset.Now, SelectedPeriod, newSymbol.Id);

            var data = new List<Ohlc>();

            foreach (var trendbar in trendbars)
            {
                data.Add(new Ohlc
                {
                    Low = newSymbol.GetPriceFromRelative(trendbar.Low),
                    High = newSymbol.GetPriceFromRelative(trendbar.Low + (long)trendbar.DeltaHigh),
                    Open = newSymbol.GetPriceFromRelative(trendbar.Low + (long)trendbar.DeltaOpen),
                    Close = newSymbol.GetPriceFromRelative(trendbar.Low + (long)trendbar.DeltaClose),
                    Time = DateTimeOffset.FromUnixTimeSeconds(trendbar.UtcTimestampInMinutes * 60)
                });
            }

            Chart.LoadData(newSymbol.Name, data);
        }

        private void SelectedSymbol_Tick(SymbolModel obj)
        {
        }
    }
}