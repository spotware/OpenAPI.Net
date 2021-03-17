using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using Trading.UI.Demo.Models;
using Trading.UI.Demo.Services;

namespace Trading.UI.Demo.ViewModels
{
    public class ApiConfigurationViewModel : ViewModelBase, IDialogAware
    {
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IAppDispatcher _dispatcher;
        private ApiConfigurationModel _model;

        public ApiConfigurationViewModel(IDialogCoordinator dialogCordinator, IAppDispatcher dispatcher)
        {
            _dialogCordinator = dialogCordinator;
            _dispatcher = dispatcher;

            DoneCommand = new DelegateCommand(Done);
        }

        public DelegateCommand DoneCommand { get; }

        public ApiConfigurationModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        public string Title { get; } = "API Configuration";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
            if (IsModelValid() is false) _dispatcher.InvokeShutdown();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("Model", out ApiConfigurationModel model)) Model = model;
        }

        private async void Done()
        {
            if (IsModelValid())
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }
            else
            {
                await _dialogCordinator.ShowMessageAsync(this, "Error",
                    "Please provide your Open API applicaiton data, some fields doesn't contain valid data");
            }
        }

        private bool IsModelValid() => !string.IsNullOrWhiteSpace(Model.ClientId)
                && !string.IsNullOrWhiteSpace(Model.Secret)
                && !string.IsNullOrWhiteSpace(Model.RedirectUri);
    }
}