using Prism.Commands;
using Prism.Services.Dialogs;
using Trading.UI.Demo.Views;

namespace Trading.UI.Demo.ViewModels
{
    public class OrdersViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;

        public OrdersViewModel(IDialogService dialogService)
        {
            CreateNewOrderCommand = new DelegateCommand(CreateNewOrder);
            CreateNewPositionCommand = new DelegateCommand(CreateNewPosition);

            _dialogService = dialogService;
        }

        public DelegateCommand CreateNewOrderCommand { get; }

        public DelegateCommand CreateNewPositionCommand { get; }

        private void CreateNewPosition()
        {
            _dialogService.ShowDialog(nameof(CreateOrderView), args =>
            {
            });
        }

        private void CreateNewOrder()
        {
            _dialogService.ShowDialog(nameof(CreateOrderView));
        }
    }
}