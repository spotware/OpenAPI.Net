using Prism.Services.Dialogs;
using System;

namespace Trading.UI.Demo.ViewModels
{
    public class CreateOrderViewModel : ViewModelBase, IDialogAware
    {
        public CreateOrderViewModel()
        {
        }

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