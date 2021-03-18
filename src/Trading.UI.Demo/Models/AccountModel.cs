namespace Trading.UI.Demo.Models
{
    public class AccountModel
    {
        public long Id { init; get; }

        public bool IsLive { init; get; }

        public SymbolModel[] Symbols { init; get; }
    }
}