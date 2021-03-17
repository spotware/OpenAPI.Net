using Prism.Commands;
using Prism.Services.Dialogs;
using System.Collections.Generic;
using Trading.UI.Demo.Models;

namespace Trading.UI.Demo.ViewModels
{
    public class CreateOrderViewModel : DialogAwareViewBase
    {
        private bool _isModifyingMarketOrder;
        private bool _isModifyingPendingOrder;
        private MarketOrderModel _marketOrderModel;
        private PendingOrderModel _pendingOrderModel;
        private List<SymbolModel> _symbols;

        public CreateOrderViewModel()
        {
            Title = "Create New Order";

            PlaceMarketOrderCommand = new DelegateCommand(PlaceMarketOrder);

            ModifyMarketOrderCommand = new DelegateCommand(ModifyMarketOrder);

            PlacePendingOrderCommand = new DelegateCommand(PlacePendingOrder);

            ModifyPendingOrderCommand = new DelegateCommand(ModifyPendingOrder);
        }

        public DelegateCommand PlaceMarketOrderCommand { get; }

        public DelegateCommand ModifyMarketOrderCommand { get; }

        public DelegateCommand PlacePendingOrderCommand { get; }

        public DelegateCommand ModifyPendingOrderCommand { get; }

        public bool IsModifyingMarketOrder { get => _isModifyingMarketOrder; set => SetProperty(ref _isModifyingMarketOrder, value); }

        public bool IsModifyingPendingOrder { get => _isModifyingPendingOrder; set => SetProperty(ref _isModifyingPendingOrder, value); }

        public MarketOrderModel MarketOrderModel { get => _marketOrderModel; set => SetProperty(ref _marketOrderModel, value); }

        public PendingOrderModel PendingOrderModel { get => _pendingOrderModel; set => SetProperty(ref _pendingOrderModel, value); }

        public List<SymbolModel> Symbols { get => _symbols; set => SetProperty(ref _symbols, value); }

        public override void OnDialogClosed()
        {
            MarketOrderModel = null;
            PendingOrderModel = null;

            IsModifyingMarketOrder = false;
            IsModifyingPendingOrder = false;

            Symbols = null;
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue<IEnumerable<SymbolModel>>("Symbols", out var symbols))
            {
                Symbols = new List<SymbolModel>(symbols);
            }

            if (parameters.TryGetValue<MarketOrderModel>("MarketOrder", out var marketOrderModel))
            {
                MarketOrderModel = marketOrderModel;

                IsModifyingMarketOrder = true;
            }
            else if (parameters.TryGetValue<PendingOrderModel>("PendingOrder", out var pendingOrderModel))
            {
                PendingOrderModel = pendingOrderModel;

                IsModifyingPendingOrder = true;
            }
            else
            {
                MarketOrderModel = new MarketOrderModel();
                PendingOrderModel = new PendingOrderModel();
            }
        }

        private void PlaceMarketOrder()
        {
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "MarketOrder", MarketOrderModel }
            }));
        }

        private void ModifyMarketOrder()
        {
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "MarketOrder", MarketOrderModel }
            }));
        }

        private void ModifyPendingOrder()
        {
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "PendingOrder", PendingOrderModel }
            }));
        }

        private void PlacePendingOrder()
        {
            OnRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "PendingOrder", PendingOrderModel }
            }));
        }
    }
}