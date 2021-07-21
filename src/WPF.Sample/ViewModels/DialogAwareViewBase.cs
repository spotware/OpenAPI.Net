using Prism.Services.Dialogs;
using System;

namespace Trading.UI.Sample.ViewModels
{
    public abstract class DialogAwareViewBase : ViewModelBase, IDialogAware
    {
        public string Title { get; protected set; }

        public event Action<IDialogResult> RequestClose;

        public virtual bool CanCloseDialog() => true;

        public virtual void OnDialogClosed()
        {
        }

        public virtual void OnDialogOpened(IDialogParameters parameters)
        {
        }

        protected void OnRequestClose(IDialogResult dialogResult) => RequestClose?.Invoke(dialogResult);
    }
}