using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Regions;
using System;

namespace Trading.UI.Demo.ViewModels
{
    public abstract class ViewModelBase : BindableBase, IConfirmNavigationRequest, IDestructible
    {
        public ViewModelBase()
        {
            LoadedCommand = new DelegateCommand(Loaded);

            UnloadedCommand = new DelegateCommand(Unloaded);
        }

        #region Properties

        public NavigationContext NavigationContext { get; private set; }

        #endregion Properties

        #region Commands

        public DelegateCommand LoadedCommand { get; }

        public DelegateCommand UnloadedCommand { get; }

        #endregion Commands

        #region INavigationAware

        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext,
            Action<bool> continuationCallback)
        {
            continuationCallback?.Invoke(true);
        }

        public virtual void Destroy()
        {
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            NavigationContext = navigationContext;
        }

        #endregion INavigationAware

        #region Loaded/unloaded

        protected virtual void Loaded()
        {
        }

        protected virtual void Unloaded()
        {
        }

        #endregion Loaded/unloaded
    }
}