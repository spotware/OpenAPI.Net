using Prism.Ioc;
using System.Windows;
using Trading.UI.Demo.Views;

namespace Trading.UI.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<ShellView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<OrdersView>();
            containerRegistry.RegisterForNavigation<CreateOrderView>();
        }
    }
}