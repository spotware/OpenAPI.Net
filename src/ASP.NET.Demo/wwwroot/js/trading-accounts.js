$(document).ready(function () {
    $(".dropdown-toggle").dropdown();

    var isLoaded = false;

    $('#accountLoadingModal').on('shown.bs.modal', function () {
        if (isLoaded) {
            isLoaded = false;
            $('#accountLoadingModal').modal('hide');
        }
    })

    $('.nav-link').on('click', e => {
        if (e.target.id == 'marketOrderTab') {
            $('#orderModal').data('type', 'createMarket');
        }
        else {
            $('#orderModal').data('type', 'createPending');
        }
    });

    var tradingAccountConnection = new signalR.HubConnectionBuilder().withUrl("/tradingAccountHub").build();

    tradingAccountConnection.start().then(function () {
        $("#accounts-list").on("change", onAccountChanged);
        $('#marketOrderSymbolsList').on("change", onMarketOrderSymbolChanged);
        $('#pendingOrderSymbolsList').on("change", onPendingOrderSymbolChanged);
        $('#marketRange').on("change", () => $('#marketRangeInput').prop('disabled', !$('#marketRange').is(":checked")));
        $('#marketOrderStopLoss').on("change", () => {
            var isDisabled = !$('#marketOrderStopLoss').is(":checked");

            $('#marketOrderStopLossInput').prop('disabled', isDisabled);
            $('#marketOrderTrailingStopLoss').prop('disabled', isDisabled);
        });
        $('#pendingOrderStopLoss').on("change", () => {
            var isDisabled = !$('#pendingOrderStopLoss').is(":checked");

            $('#pendingOrderStopLossInput').prop('disabled', isDisabled);
            $('#pendingOrderTrailingStopLoss').prop('disabled', isDisabled);
        });
        $('#marketOrderTakeProfit').on("change", () => $('#marketOrderTakeProfitInput').prop('disabled', !$('#marketOrderTakeProfit').is(":checked")));
        $('#pendingOrderTakeProfit').on("change", () => $('#pendingOrderTakeProfitInput').prop('disabled', !$('#pendingOrderTakeProfit').is(":checked")));
        $('#pendingOrderExpiry').on("change", () => $('#pendingOrderExpiryDateTime').prop('disabled', !$('#pendingOrderExpiry').is(":checked")));
        $('#pendingOrderTypeList').on("change", () => $('#pendingOrderLimitRangeInput').prop('disabled', $('#pendingOrderTypeList').val() != "StopLimit"));

        onAccountChanged();
    }).catch(onError);

    tradingAccountConnection.on("AccountLoaded", function (accountLogin) {
        tradingAccountConnection.stream("GetErrors", accountLogin)
            .subscribe({
                next: onError,
                complete: () => {
                    console.info("Errors completed");
                },
                error: onError,
            });

        tradingAccountConnection.invoke("GetSymbols", accountLogin).catch(onError);

        tradingAccountConnection.invoke("GetPositions", accountLogin).catch(onError);

        tradingAccountConnection.invoke("GetOrders", accountLogin).catch(onError);

        tradingAccountConnection.invoke("GetAccountInfo", accountLogin).catch(onError);

        event.preventDefault();

        $('#accountLoadingModal').modal('hide');

        isLoaded = true;
    });

    tradingAccountConnection.on("Positions", function (data) {
        var rows = '';
        $.each(data.positions, function (i, position) {
            var row = `<tr id="${position.id}">${getPositionRowData(position)}</tr>`;

            rows += row;
        });

        $('#positions-table-body').html(rows);

        tradingAccountConnection.stream("GetPositionUpdates", data.accountLogin)
            .subscribe({
                next: (position) => {
                    var row = $('#positions-table-body').find(`#${position.id}`);

                    if (position.volume == 0) {
                        row.remove();
                        return;
                    }
                    else if (row.length == 0) {
                        newRow = `<tr id="${position.id}">${getPositionRowData(position)}</tr>`;

                        $('#positions-table-body').append(newRow);

                        return;
                    }
                    else {
                        row.html(getPositionRowData(position));
                    }
                },
                complete: () => {
                    console.info("Position Updates completed");
                },
                error: onError,
            });

        event.preventDefault();
    });

    tradingAccountConnection.on("Orders", function (data) {
        var rows = '';
        $.each(data.orders, function (i, order) {
            var row = `<tr id="${order.id}">${getOrderRowData(order)}</tr>`;

            rows += row;
        });

        $('#orders-table-body').html(rows);

        tradingAccountConnection.stream("GetOrderUpdates", data.accountLogin)
            .subscribe({
                next: (order) => {
                    var row = $('#orders-table-body').find(`#${order.id}`);

                    if (order.isFilledOrCanceled) {
                        row.remove();
                        return;
                    }
                    else if (row.length == 0) {
                        newRow = `<tr id="${order.id}">${getOrderRowData(order)}</tr>`;

                        $('#orders-table-body').append(newRow);

                        return;
                    }
                    else {
                        row.html(getOrderRowData(order));
                    }
                },
                complete: () => {
                    console.info("Order Updates completed");
                },
                error: onError,
            });

        event.preventDefault();
    });

    tradingAccountConnection.on("Symbols", function (data) {
        var rows = '';
        $.each(data.symbols, function (i, symbol) {
            $("#symbolsTable").data(symbol.name, symbol);

            var row = `<tr id="${symbol.id}">
                            <td id="name">${symbol.name}</td>
                            <td id="bid">${symbol.bid}</td>
                            <td id="ask">${symbol.ask}</td></tr>`;

            rows += row;
        });

        $('#symbolsTableBody').html(rows);

        tradingAccountConnection.stream("GetSymbolQuotes", data.accountLogin)
            .subscribe({
                next: (quote) => {
                    var bid = $('#symbolsTableBody > #' + quote.id + ' > #bid');
                    var ask = $('#symbolsTableBody > #' + quote.id + ' > #ask');

                    bid.html(quote.bid);
                    ask.html(quote.ask);
                },
                complete: () => {
                    console.info("Symbol Quotes completed");
                },
                error: onError,
            });

        event.preventDefault();
    });

    tradingAccountConnection.on("AccountInfo", function (data) {
        updateAccountStats(data.info);

        tradingAccountConnection.stream("GetAccountInfoUpdates", data.accountLogin)
            .subscribe({
                next: (info) => {
                    updateAccountStats(info);
                },
                complete: () => {
                    console.info("Account Info Updates completed");
                },
                error: onError,
            });

        event.preventDefault();
    });

    $(document).on("click", ".close-position", function () {
        tradingAccountConnection.invoke("ClosePosition", $("#accounts-list").val(), $(this).attr('id')).catch(onError);
    });

    $(document).on("click", "#closeAllPositionsButton", function () {
        tradingAccountConnection.invoke("CloseAllPositions", $("#accounts-list").val()).catch(onError);
    });

    $(document).on("click", ".modify-position", function () {
        var positionId = parseInt($(this).attr('id'));
        var accountId = parseInt($("#accounts-list").val());

        tradingAccountConnection.invoke('getPositionInfo', accountId, positionId).then(info => {
            resetOrderForms();

            $('#pendingOrderTab').addClass('disabled');

            fillSymbolsList();

            $('#marketOrderSymbolsList').val(info.symbolName).change();
            $('#marketOrderSymbolsList').prop('disabled', true);
            $('#marketOrderDirectionList').val(info.direction).change();
            $('#marketOrderVolumeInput').val(info.volume).change();
            $('#marketRangeInput').removeAttr('value');
            $('#marketRangeInput').prop('disabled', true);
            $('#marketRange').prop('disabled', true);
            $("#marketOrderStopLoss").prop("checked", info.hasStopLoss);
            $('#marketOrderStopLossInput').prop('disabled', !info.hasStopLoss);
            $("#marketOrderTakeProfit").prop("checked", info.hasTakeProfit);
            $('#marketOrderTakeProfitInput').prop('disabled', !info.hasTakeProfit);
            $('#marketOrderStopLossInput').val(info.stopLossInPips).change();
            $('#marketOrderTakeProfitInput').val(info.takeProfitInPips).change();
            $('#marketOrderTrailingStopLoss').prop('disabled', !info.hasStopLoss);
            $("#marketOrderTrailingStopLoss").prop("checked", info.hasTrailingStop);
            $('#marketOrderCommenttextarea').val(info.comment).change();
            $('#marketOrderCommenttextarea').prop('disabled', true);

            $('#orderModalButton').html("Modify Order");
            $('#orderModalTitle').html("Modify Market Order");

            $('#orderModal').data('type', 'modifyMarket');
            $('#orderModal').data('id', info.id);

            $('#marketOrderTab').tab('show');

            $('#orderModal').modal('toggle');
        }).catch(onError);
    });

    $(document).on("click", ".cancel-order", function () {
        tradingAccountConnection.invoke("CancelOrder", $("#accounts-list").val(), $(this).attr('id')).catch(onError);
    });

    $(document).on("click", "#cancelAllOrdersButton", function () {
        tradingAccountConnection.invoke("CancelAllOrders", $("#accounts-list").val()).catch(onError);
    });

    $(document).on("click", ".modify-order", function () {
        var positionId = parseInt($(this).attr('id'));
        var accountId = parseInt($("#accounts-list").val());

        tradingAccountConnection.invoke('getOrderInfo', accountId, positionId).then(info => {
            resetOrderForms();

            $('#marketOrderTab').addClass('disabled');

            fillSymbolsList();

            $('#pendingOrderSymbolsList').val(info.symbolName).change();
            $('#pendingOrderSymbolsList').prop('disabled', true);
            $('#pendingOrderDirectionList').val(info.direction).change();
            $('#pendingOrderDirectionList').prop('disabled', true);
            $('#pendingOrderVolumeInput').val(info.volume).change();
            $('#pendingOrderTypeList').val(info.type).change();
            $('#pendingOrderTypeList').prop('disabled', true);
            $("#pendingOrderStopLoss").prop("checked", info.hasStopLoss);
            $('#pendingOrderStopLossInput').prop('disabled', !info.hasStopLoss);
            $("#pendingOrderTakeProfit").prop("checked", info.hasTakeProfit);
            $('#pendingOrderTakeProfitInput').prop('disabled', !info.hasTakeProfit);
            $('#pendingOrderStopLossInput').val(info.stopLossInPips).change();
            $('#pendingOrderTakeProfitInput').val(info.takeProfitInPips).change();
            $('#pendingOrderTrailingStopLoss').prop('disabled', !info.hasStopLoss);
            $("#pendingOrderTrailingStopLoss").prop("checked", info.hasTrailingStop);
            $('#pendingOrderCommenttextarea').val(info.comment).change();
            $('#pendingOrderCommenttextarea').prop('disabled', true);
            $('#pendingOrderExpiry').prop('checked', info.hasExpiry);
            $('#pendingOrderExpiryDateTime').prop('disabled', !info.hasExpiry);

            if (info.hasExpiry) {
                $('#pendingOrderExpiryDateTime').val(new Date(info.expiry).toJSON().slice(0, 19)).change();
            }

            $('#pendingOrderPriceInput').val(info.price).change();

            $('#orderModalButton').html("Modify Order");
            $('#orderModalTitle').html("Modify Pending Order");

            $('#orderModal').data('type', 'modifyPending');
            $('#orderModal').data('id', info.id);

            $('#pendingOrderTab').tab('show');

            $('#orderModal').modal('toggle');
        }).catch(onError);
    });

    $(document).on("click", "#createMarketOrderButton", function () {
        resetOrderForms();

        fillSymbolsList();

        $('#orderModalButton').html("Place Order");
        $('#orderModalTitle').html("Create New Order");

        $('#orderModal').data('type', 'createMarket');

        $('#marketOrderTab').tab('show');

        $('#orderModal').modal('toggle');
    });

    $(document).on("click", "#createPendingOrderButton", function () {
        resetOrderForms();

        fillSymbolsList();

        $('#orderModalButton').html("Place Order");
        $('#orderModalTitle').html("Create New Order");

        $('#orderModal').data('type', 'createPending');

        $('#pendingOrderTab').tab('show');

        $('#orderModal').modal('toggle');
    });

    $(document).on("click", "#orderModalButton", function () {
        switch ($('#orderModal').data('type')) {
            case "createMarket":
                tradingAccountConnection.invoke("CreateNewMarketOrder", {
                    "accountLogin": parseInt($("#accounts-list").val()),
                    "symbolId": $("#symbolsTable").data($('#marketOrderSymbolsList').val()).id,
                    "volume": parseFloat($('#marketOrderVolumeInput').val()),
                    "direction": $('#marketOrderDirectionList').val(),
                    "comment": $('#marketOrderCommenttextarea').val(),
                    "isMarketRange": $('#marketRange').is(":checked"),
                    "marketRange": parseInt($('#marketRangeInput').val()),
                    "hasStopLoss": $('#marketOrderStopLoss').is(":checked"),
                    "stopLoss": parseInt($('#marketOrderStopLossInput').val()),
                    "hasTrailingStop": $('#marketOrderTrailingStopLoss').is(":checked"),
                    "hasTakeProfit": $('#marketOrderTakeProfit').is(":checked"),
                    "takeProfit": parseInt($('#marketOrderTakeProfitInput').val())
                }).catch(onError);

                break;
            case "modifyMarket":
                tradingAccountConnection.invoke("ModifyMarketOrder", {
                    "accountLogin": parseInt($("#accounts-list").val()),
                    "id": $('#orderModal').data('id'),
                    "volume": parseFloat($('#marketOrderVolumeInput').val()),
                    "direction": $('#marketOrderDirectionList').val(),
                    "hasStopLoss": $('#marketOrderStopLoss').is(":checked"),
                    "stopLoss": parseInt($('#marketOrderStopLossInput').val()),
                    "hasTrailingStop": $('#marketOrderTrailingStopLoss').is(":checked"),
                    "hasTakeProfit": $('#marketOrderTakeProfit').is(":checked"),
                    "takeProfit": parseInt($('#marketOrderTakeProfitInput').val())
                }).catch(onError);
                break;

            case "createPending":
                tradingAccountConnection.invoke("CreateNewPendingOrder", {
                    "accountLogin": parseInt($("#accounts-list").val()),
                    "symbolId": $("#symbolsTable").data($('#pendingOrderSymbolsList').val()).id,
                    "volume": parseFloat($('#pendingOrderVolumeInput').val()),
                    "direction": $('#pendingOrderDirectionList').val(),
                    "comment": $('#pendingOrderCommenttextarea').val(),
                    "hasStopLoss": $('#pendingOrderStopLoss').is(":checked"),
                    "stopLoss": parseInt($('#pendingOrderStopLossInput').val()),
                    "hasTrailingStop": $('#pendingOrderTrailingStopLoss').is(":checked"),
                    "hasTakeProfit": $('#pendingOrderTakeProfit').is(":checked"),
                    "takeProfit": parseInt($('#pendingOrderTakeProfitInput').val()),
                    "type": $('#pendingOrderTypeList').val(),
                    "price": parseFloat($('#pendingOrderPriceInput').val()),
                    "limitRange": parseInt($('#pendingOrderLimitRangeInput').val()),
                    "hasExpiry": $('#pendingOrderExpiry').is(":checked"),
                    "expiry": $('#pendingOrderExpiry').is(":checked") ? new Date($('#pendingOrderExpiryDateTime').val()) : new Date()
                }).catch(onError);
                break;

            case "modifyPending":
                tradingAccountConnection.invoke("ModifyPendingOrder", {
                    "accountLogin": parseInt($("#accounts-list").val()),
                    "id": $('#orderModal').data('id'),
                    "volume": parseFloat($('#pendingOrderVolumeInput').val()),
                    "hasStopLoss": $('#pendingOrderStopLoss').is(":checked"),
                    "stopLoss": parseInt($('#pendingOrderStopLossInput').val()),
                    "hasTrailingStop": $('#pendingOrderTrailingStopLoss').is(":checked"),
                    "hasTakeProfit": $('#pendingOrderTakeProfit').is(":checked"),
                    "takeProfit": parseInt($('#pendingOrderTakeProfitInput').val()),
                    "price": parseFloat($('#pendingOrderPriceInput').val()),
                    "limitRange": parseInt($('#pendingOrderLimitRangeInput').val()),
                    "hasExpiry": $('#pendingOrderExpiry').is(":checked"),
                    "expiry": $('#pendingOrderExpiry').is(":checked") ? new Date($('#pendingOrderExpiryDateTime').val()) : new Date()
                }).catch(onError);
                break;

            default:
                alert('Unknown case ' + $('#orderModal').data('type'));
                break;
        }

        $('#orderModal').modal('hide');
    });

    $(document).on("click", "#closeOrderModalButton", function () {
        $('#orderModal').modal('hide');
    });

    function onAccountChanged() {
        isLoaded = false;

        $("#accountLoadingModal").modal({
            backdrop: 'static',
            keyboard: false
        });

        $('#accountLoadingModal').modal('toggle')

        var previousAccountLogin = $("#accounts-list").data("previousAccountLogin");

        tradingAccountConnection.invoke("StopSymbolQuotes", previousAccountLogin).catch(onError);

        tradingAccountConnection.invoke("StopPositionUpdates", previousAccountLogin).catch(onError);

        tradingAccountConnection.invoke("StopOrderUpdates", previousAccountLogin).catch(onError);

        tradingAccountConnection.invoke("StopAccountInfoUpdates", previousAccountLogin).catch(onError);

        tradingAccountConnection.invoke("StopErrors", previousAccountLogin).catch(onError);

        var accountLogin = $("#accounts-list").val();

        $("#accounts-list").data("previousAccountLogin", accountLogin);

        tradingAccountConnection.invoke("LoadAccount", accountLogin).catch(onError);

        event.preventDefault();
    };

    function getPositionRowData(position) {
        return `<td id="id">${position.id}</td>
                <td id="symbol">${position.symbol}</td>
                <td id="direction">${position.direction}</td>
                <td id="volume">${position.volume}</td>
                <td id="openTime">${position.openTime}</td>
                <td id="price">${position.price}</td>
                <td id="stopLoss">${position.stopLoss}</td>
                <td id="takeProfit">${position.takeProfit}</td>
                <td id="commission">${position.commission}</td>
                <td id="swap">${position.swap}</td>
                <td id="margin">${position.margin}</td>
                <td id="pips">${position.pips}</td>
                <td id="label">${position.label}</td>
                <td id="comment">${position.comment}</td>
                <td id="grossProfit">${position.grossProfit}</td>
                <td id="netProfit">${position.netProfit}</td>
                <td id="buttons">
                    <button type="button" class="modify-position btn btn-secondary mr-1" id="${position.id}" data-bs-toggle="tooltip" data-bs-placement="top" title="Modify"><i class="fas fa-edit"></i></button>
                    <button type="button" class="close-position btn btn-danger ml-1" id="${position.id}" data-bs-toggle="tooltip" data-bs-placement="top" title="Close"><i class="fas fa-times"></i></button>
                </td>`;
    }

    function getOrderRowData(order) {
        return `<td id="id">${order.id}</td>
                <td id="symbol">${order.symbol}</td>
                <td id="direction">${order.direction}</td>
                <td id="volume">${order.volume}</td>
                <td id="volume">${order.type}</td>
                <td id="openTime">${order.openTime}</td>
                <td id="price">${order.price}</td>
                <td id="stopLoss">${order.stopLoss}</td>
                <td id="takeProfit">${order.takeProfit}</td>
                <td id="expiry">${order.isExpiryEnabled ? order.expiry : ""}</td>
                <td id="label">${order.label}</td>
                <td id="comment">${order.comment}</td>
                <td id="buttons">
                    <button type="button" class="modify-order btn btn-secondary mr-1" id="${order.id}" data-bs-toggle="tooltip" data-bs-placement="top" title="Modify"><i class="fas fa-edit"></i></button>
                    <button type="button" class="cancel-order btn btn-danger ml-1" id="${order.id}" data-bs-toggle="tooltip" data-bs-placement="top" title="Cancel"><i class="fas fa-times"></i></button>
                </td>`;
    }

    function updateAccountStats(info) {
        $('#balance').html(`${info.balance} ${info.currency}`);
        $('#equity').html(`${info.equity} ${info.currency}`);
        $('#marginUsed').html(`${info.marginUsed} ${info.currency}`);
        $('#freeMargin').html(`${info.freeMargin} ${info.currency}`);
        $('#marginLevel').html(`${info.marginLevel > 0 ? info.marginLevel : "N/A"}%`);
        $('#unrealizedGrossProfit').html(`${info.unrealizedGrossProfit} ${info.currency}`);
        $('#unrealizedNetProfit').html(`${info.unrealizedNetProfit} ${info.currency}`);
    }

    function onError(error) {
        console.error(`Error: ${error}`);

        var toastTemplate = $('#toast-template').contents().clone(true, true);

        toastTemplate.find('#toast-title').text('Error');
        toastTemplate.find('#toast-title-small').text(error.hasOwnProperty('type') ? error.type : 'N/A');
        toastTemplate.find('#toast-icon').addClass('fas fa-exclamation-triangle');
        toastTemplate.find('.toast-body').text(error.hasOwnProperty('message') ? error.message : error);

        var toast = toastTemplate.find(".toast");

        $('#toasts-container').append(toastTemplate);

        toast.toast({
            delay: 60000
        });

        $('.toast').toast('show');

        $('.toast').on('hidden.bs.toast', e => e.target.remove());
    }

    function onMarketOrderSymbolChanged() {
        var selectedSymbolName = $('#marketOrderSymbolsList').val();

        var symbol = $("#symbolsTable").data(selectedSymbolName);

        $("#marketOrderVolumeInput").attr({
            "max": symbol.maxVolume,
            "min": symbol.minVolume,
            "step": symbol.stepVolume
        }).change();

        $("#marketOrderVolumeInput").val(symbol.minVolume).change();
    }

    function onPendingOrderSymbolChanged() {
        var selectedSymbolName = $('#pendingOrderSymbolsList').val();

        var symbol = $("#symbolsTable").data(selectedSymbolName);

        $("#pendingOrderVolumeInput").attr({
            "max": symbol.maxVolume,
            "min": symbol.minVolume,
            "step": symbol.stepVolume
        }).change();

        $("#pendingOrderPriceInput").attr({
            "step": symbol.pipSize,
        }).change();

        $("#pendingOrderPriceInput").val(getSymbolPrice(symbol.id)).change();
        $("#pendingOrderVolumeInput").val(symbol.minVolume).change();
    }

    function fillSymbolsList() {
        var symbolOptions = '';

        $('#symbolsTableBody > tr').each((index, element) => {
            var name = $(element).find('#name').text();

            symbolOptions += `<option value="${name}" id="${element.id}">${name}</option>`;
        });

        $('#marketOrderSymbolsList').html(symbolOptions);
        $('#pendingOrderSymbolsList').html(symbolOptions);

        onMarketOrderSymbolChanged();
        onPendingOrderSymbolChanged();
    }

    function resetMarketOrderForm() {
        $("#marketOrderSymbolsList").prop('selectedIndex', 0)
        $('#marketOrderSymbolsList').prop('disabled', false);
        $('#marketOrderDirectionList').val('Buy').change();
        $('#marketOrderVolumeInput').val(1).change();
        $('#marketRangeInput').prop('value', 10);
        $('#marketRangeInput').prop('disabled', true);
        $('#marketRange').prop('disabled', false);
        $("#marketRange").prop("checked", false);
        $("#marketOrderStopLoss").prop("checked", false);
        $('#marketOrderStopLossInput').prop('disabled', true);
        $("#marketOrderTakeProfit").prop("checked", false);
        $('#marketOrderTakeProfitInput').prop('disabled', true);
        $('#marketOrderStopLossInput').val(20).change();
        $('#marketOrderTakeProfitInput').val(20).change();
        $('#marketOrderTrailingStopLoss').prop('disabled', true);
        $("#marketOrderTrailingStopLoss").prop("checked", false);
        $('#marketOrderCommenttextarea').val('').change();
        $('#marketOrderCommenttextarea').prop('disabled', false);
    }

    function resetPendingOrderForm() {
        $("#pendingOrderSymbolsList").prop('selectedIndex', 0)
        $('#pendingOrderSymbolsList').prop('disabled', false);
        $('#pendingOrderDirectionList').val('Buy').change();
        $('#pendingOrderVolumeInput').val(1).change();
        $('#pendingOrderPriceInput').val(0).change();
        $('#pendingOrderTypeList').val('Limit').change();
        $('#pendingOrderLimitRangeInput').prop('value', 10);
        $('#pendingOrderLimitRangeInput').prop('disabled', true);
        $("#pendingOrderExpiry").prop("checked", false);
        $('#pendingOrderExpiryDateTime').val(new Date().toJSON().slice(0, 19)).change();
        $('#pendingOrderExpiryDateTime').prop('disabled', true);
        $('#pendingOrderExpiryDateTime').prop('min', new Date().toJSON().slice(0, 19));
        $("#pendingOrderStopLoss").prop("checked", false);
        $('#pendingOrderStopLossInput').prop('disabled', true);
        $("#pendingOrderTakeProfit").prop("checked", false);
        $('#pendingOrderTakeProfitInput').prop('disabled', true);
        $('#pendingOrderStopLossInput').val(20).change();
        $('#pendingOrderTakeProfitInput').val(20).change();
        $('#pendingOrderTrailingStopLoss').prop('disabled', true);
        $("#pendingOrderTrailingStopLoss").prop("checked", false);
        $('#pendingOrderCommenttextarea').val('').change();
        $('#pendingOrderCommenttextarea').prop('disabled', false);
    }

    function resetOrderForms() {
        $('#marketOrderTab').removeClass('disabled');
        $('#pendingOrderTab').removeClass('disabled');

        $('#orderModal').data('type', 'createMarket');
        $('#orderModal').data('id', 0);

        resetMarketOrderForm();
        resetPendingOrderForm();
    }

    function getSymbolPrice(symbolId) {
        return $("#symbolsTableBody").find(`#${symbolId}`).find('#bid').text();
    }
});