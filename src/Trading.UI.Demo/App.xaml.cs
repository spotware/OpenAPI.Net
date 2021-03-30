using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using OpenAPI.Net;
using OpenAPI.Net.Helpers;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Windows;
using Trading.UI.Demo.Events;
using Trading.UI.Demo.Helpers;
using Trading.UI.Demo.Services;
using Trading.UI.Demo.Views;

namespace Trading.UI.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell() => Container.Resolve<ShellView>();

        protected override void InitializeShell(Window window)
        {
            var eventAggregator = Container.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<ChangeThemeEvent>().Subscribe(ChangeThemeEvent_Handler, ThreadOption.UIThread);

            window.Show();
        }

        private void ChangeThemeEvent_Handler()
        {
            var currentTheme = ThemeManager.Current.DetectTheme(this)?.Name.Split('.')[0];

            string newTheme = string.Equals(currentTheme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";

            ThemeManager.Current.ChangeTheme(this, $"{newTheme}.Steel");
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IDialogCoordinator, DialogCoordinator>();

            // Views
            containerRegistry.RegisterForNavigation<AccountDataView>();
            containerRegistry.RegisterForNavigation<ChartView>();

            // Dialogs
            containerRegistry.RegisterDialogWindow<DialogWindow>();

            containerRegistry.RegisterDialog<ApiConfigurationView>();
            containerRegistry.RegisterDialog<CreateModifyOrderView>();

            // Services
            OpenClient liveClientFactory() => new OpenClient(ApiInfo.LiveHost, ApiInfo.Port, TimeSpan.FromSeconds(10));
            OpenClient demoClientFactory() => new OpenClient(ApiInfo.DemoHost, ApiInfo.Port, TimeSpan.FromSeconds(10));

            var apiService = new ApiService(liveClientFactory, demoClientFactory);

            containerRegistry.RegisterInstance<IApiService>(apiService);

            containerRegistry.RegisterInstance<IAppDispatcher>(new AppDispatcher(Current.Dispatcher));
        }
    }
}