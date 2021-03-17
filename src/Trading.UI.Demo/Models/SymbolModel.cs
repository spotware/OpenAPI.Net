using Prism.Mvvm;

namespace Trading.UI.Demo.Models
{
    public class SymbolModel : BindableBase
    {
        private string _name;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
    }
}