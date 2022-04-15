using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Trading.UI.Sample.Models
{
    public class AccountModel : BindableBase
    {
        private double _unrealizedNetProfit;
        private double _unrealizedGrossProfit;
        private double _marginLevel;
        private double _freeMargin;
        private double _marginUsed;
        private double _equity;
        private double _balance;

        public long Id { get; init; }

        public bool IsLive { get; init; }

        public SymbolModel[] Symbols { get; init; }

        public ProtoOATrader Trader { get; init; }

        public DateTimeOffset RegistrationTime { get; init; }

        public ProtoOAAsset DepositAsset { get; init; }

        public string Currency => DepositAsset.Name;

        public IReadOnlyCollection<ProtoOAAsset> Assets { get; init; }

        public double Balance { get => _balance; set => SetProperty(ref _balance, value); }

        public double Equity { get => _equity; set => SetProperty(ref _equity, value); }

        public double MarginUsed { get => _marginUsed; set => SetProperty(ref _marginUsed, value); }

        public double FreeMargin { get => _freeMargin; set => SetProperty(ref _freeMargin, value); }

        public double MarginLevel { get => _marginLevel; set => SetProperty(ref _marginLevel, value); }

        public double UnrealizedGrossProfit { get => _unrealizedGrossProfit; set => SetProperty(ref _unrealizedGrossProfit, value); }

        public double UnrealizedNetProfit { get => _unrealizedNetProfit; set => SetProperty(ref _unrealizedNetProfit, value); }

        public ObservableCollection<MarketOrderModel> Positions { get; } = new ObservableCollection<MarketOrderModel>();

        public ObservableCollection<PendingOrderModel> PendingOrders { get; } = new ObservableCollection<PendingOrderModel>();

        public void UpdateStatus()
        {
            var positionsNetProfit = Positions.Sum(iPosition => iPosition.NetProfit);
            var positionsGrossProfit = Positions.Sum(iPosition => iPosition.GrossProfit);

            var positionsUsedMargin = Positions.Sum(iPosition => iPosition.UsedMargin / Math.Pow(10, iPosition.MoneyDigits));

            Equity = Math.Round(Balance + positionsNetProfit, 2);
            MarginUsed = Math.Round(positionsUsedMargin, 2);
            FreeMargin = Math.Round(Equity - MarginUsed, 2);
            MarginLevel = MarginUsed is 0 ? 0 : Math.Round(Equity / MarginUsed * 100, 2);
            UnrealizedNetProfit = Math.Round(positionsNetProfit, 2);
            UnrealizedGrossProfit = Math.Round(positionsGrossProfit, 2);
        }
    }
}