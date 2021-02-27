using Prism.Regions;
using Trading.UI.Demo.Regions;
using Trading.UI.Demo.Views;

namespace Trading.UI.Demo.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private readonly IRegionManager _regionManager;

        public ShellViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public string Title { get; } = "Trading UI Demo";

        protected override void Loaded()
        {
            _regionManager.RequestNavigate(ShellViewRegions.OrdersViewRegion, nameof(OrdersView));
        }
    }
}