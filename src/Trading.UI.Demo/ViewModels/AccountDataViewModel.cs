using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using Trading.UI.Demo.Events;
using Trading.UI.Demo.Models;
using Trading.UI.Demo.Views;

namespace Trading.UI.Demo.ViewModels
{
    public class AccountDataViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCordinator;

        private AccountModel _account;

        public AccountDataViewModel(IDialogService dialogService, IEventAggregator eventAggregator, IDialogCoordinator dialogCordinator)
        {
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;

            CreateNewOrderCommand = new DelegateCommand(ShowCreateModifyOrderDialog);
        }

        public DelegateCommand CreateNewOrderCommand { get; }

        protected override void Loaded()
        {
            _eventAggregator.GetEvent<AccountChangedEvent>().Subscribe(OnAccountChanged);
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
        }
    }
}