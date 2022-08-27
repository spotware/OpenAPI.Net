using System;

namespace OpenAPI.Net.Models
{
    public class Transaction
    {
        public long Id { get; set; }

        public ProtoOA.Enums.ChangeBalanceType Type { get; set; }

        public double Balance { get; set; }

        public double Delta { get; set; }

        public long BalanceVersion { get; set; }

        public long Equity { get; set; }

        public DateTimeOffset Time { get; set; }

        public string Note { get; set; }
        public Transaction() { }
        public Transaction(ProtoOA.Model.DepositWithdraw depositWithdraw)
        {
            Id = depositWithdraw.BalanceHistoryId;
            Type = depositWithdraw.OperationType;
            Balance = depositWithdraw.Balance;
            BalanceVersion = depositWithdraw.BalanceVersion;
            Delta = depositWithdraw.Delta;
            Equity = depositWithdraw.Equity;
            Time = DateTimeOffset.FromUnixTimeMilliseconds(depositWithdraw.ChangeBalanceTimestamp);
            Note = string.IsNullOrEmpty(depositWithdraw.ExternalNote) ? string.Empty : depositWithdraw.ExternalNote;
        }
    }
}