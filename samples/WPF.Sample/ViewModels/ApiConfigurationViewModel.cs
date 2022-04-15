using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Services.Dialogs;
using System.IO;
using Trading.UI.Sample.Events;
using Trading.UI.Sample.Models;
using Trading.UI.Sample.Services;

namespace Trading.UI.Sample.ViewModels
{
    public class ApiConfigurationViewModel : DialogAwareViewBase
    {
        private readonly IDialogCoordinator _dialogCordinator;
        private readonly IAppDispatcher _dispatcher;
        private readonly IEventAggregator _eventAggregator;
        private ApiConfigurationModel _model;

        public ApiConfigurationViewModel(IDialogCoordinator dialogCordinator, IAppDispatcher dispatcher, IEventAggregator eventAggregator)
        {
            _dialogCordinator = dialogCordinator;
            _dispatcher = dispatcher;
            _eventAggregator = eventAggregator;

            DoneCommand = new DelegateCommand(Done);

            LoadFromFileCommand = new DelegateCommand(LoadFromFile);

            Title = "API Configuration";
        }

        public DelegateCommand DoneCommand { get; }

        public DelegateCommand LoadFromFileCommand { get; }

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
            Model = new ApiConfigurationModel();
        }

        private async void Done()
        {
            if (IsModelValid())
            {
                _eventAggregator.GetEvent<ApiConfigurationFinishedEvent>().Publish(Model);

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

        private async void LoadFromFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = "(.XML)|*.xml",
                CheckPathExists = true,
                ValidateNames = true,
                Title = "Load API Configuration"
            };

            if (openFileDialog.ShowDialog() is false) return;

            if (File.Exists(openFileDialog.FileName) is false)
            {
                await _dialogCordinator.ShowMessageAsync(this, "Error", "File not found");

                return;
            }

            Model = ApiConfigurationModel.LoadFromFile(openFileDialog.FileName);
        }
    }
}