using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Windows;
using Trading.UI.Demo.Models;

namespace Trading.UI.Demo.ViewModels
{
    public class ApiConfigurationViewModel : ViewModelBase, IDialogAware
    {
        private readonly IDialogCoordinator _dialogCordinator;
        private ApiConfigurationModel _model;

        public ApiConfigurationViewModel(IDialogCoordinator dialogCordinator)
        {
            _dialogCordinator = dialogCordinator;

            DoneCommand = new DelegateCommand(Done);
            ExitCommand = new DelegateCommand(Exit);
        }

        public DelegateCommand DoneCommand { get; }

        public DelegateCommand ExitCommand { get; }

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
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue("Model", out ApiConfigurationModel model))
            {
                Model = model;
            }
        }

        private void Exit() => Application.Current.Shutdown();

        private async void Done()
        {
            if (!string.IsNullOrWhiteSpace(Model.ClientId)
                && !string.IsNullOrWhiteSpace(Model.Secret)
                && !string.IsNullOrWhiteSpace(Model.RedirectUri))
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }
            else
            {
                await _dialogCordinator.ShowMessageAsync(this, "Error",
                    "Please provide your Open API applicaiton data, some fields doesn't contain valid data");
            }
        }
    }
}