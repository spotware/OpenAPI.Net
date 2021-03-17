using MahApps.Metro.Controls.Dialogs;
using Prism.Events;
using Prism.Services.Dialogs;
using System;
using Trading.UI.Demo.Services;

namespace Trading.UI.Demo.ViewModels
{
    public class CreateOrderViewModel : ViewModelBase, IDialogAware
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IApiService _apiService;
        private bool _isModifying;

        public CreateOrderViewModel(IEventAggregator eventAggregator,
            IDialogCoordinator dialogCordinator, IApiService apiService)
        {
            _eventAggregator = eventAggregator;
            _dialogCordinator = dialogCordinator;
            _apiService = apiService;
        }

        public bool IsModifying { get => _isModifying; set => SetProperty(ref _isModifying, value); }

        public string Title { get; } = "Create New Order";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
    }
}