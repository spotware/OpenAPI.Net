using System;
using System.Collections.Generic;
using System.Linq;

namespace Samples.Shared.Models
{
    public class AccountModel
    {
        public long Id { get; init; }

        public bool IsLive { get; init; }

        public SymbolModel[] Symbols { get; init; }

        public ProtoOATrader Trader { get; init; }

        public DateTimeOffset RegistrationTime { get; init; }

        public ProtoOAAsset DepositAsset { get; init; }

        public string Currency => DepositAsset.Name;

        public IReadOnlyCollection<ProtoOAAsset> Assets { get; init; }

        public double Balance { get; set; }

        public double Equity { get; set; }

        public double MarginUsed { get; set; }

        public double FreeMargin { get; set; }

        public double MarginLevel { get; set; }

        public double UnrealizedGrossProfit { get; set; }

        public double UnrealizedNetProfit { get; set; }

        public List<MarketOrderModel> Positions { get; } = new List<MarketOrderModel>();

        public List<PendingOrderModel> PendingOrders { get; } = new List<PendingOrderModel>();

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