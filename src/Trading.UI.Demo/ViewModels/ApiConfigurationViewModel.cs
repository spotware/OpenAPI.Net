using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Services.Dialogs;
using Trading.UI.Demo.Models;
using Trading.UI.Demo.Services;

namespace Trading.UI.Demo.ViewModels
{
    public class ApiConfigurationViewModel : DialogAwareViewBase
    {
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IAppDispatcher _dispatcher;
        private ApiConfigurationModel _model;

        public ApiConfigurationViewModel(IDialogCoordinator dialogCordinator, IAppDispatcher dispatcher)
        {
            _dialogCordinator = dialogCordinator;
            _dispatcher = dispatcher;

            DoneCommand = new DelegateCommand(Done);

            Title = "API Configuration";
        }

        public DelegateCommand DoneCommand { get; }

        public ApiConfigurationModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        public override void OnDialogClosed()
        {
            if (IsModelValid() is false) _dispatcher.InvokeShutdown();
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("Model", out ApiConfigurationModel model)) Model = model;
        }

        private async void Done()
        {
            if (IsModelValid())
            {
                OnRequestClose(new DialogResult(ButtonResult.OK));
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