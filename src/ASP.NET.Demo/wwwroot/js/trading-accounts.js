$(document).ready(function () {
    $(".dropdown-toggle").dropdown();

    var isLoaded = false;

    $('#accountLoadingModal').on('shown.bs.modal', function () {
        if (isLoaded) {
            isLoaded = false;
            $('#accountLoadingModal').modal('hide');
        }
    })

    var tradingAccountConnection = new signalR.HubConnectionBuilder().withUrl("/tradingAccountHub").build();

    tradingAccountConnection.start().then(function () {
        $("#accounts-list").on("change", onAccountChanged);
        $('#marketOrderSymbolsList').on("change", onMarketOrderSymbolChanged);
        $('#marketRange').on("change", () => $('#marketRangeInput').prop('disabled', !$('#marketRange').is(":checked")));
        $('#marketOrderStopLoss').on("change", () => {
            var isDisabled = !$('#marketOrderStopLoss').is(":checked");

            $('#marketOrderStopLossInput').prop('disabled', isDisabled);
            $('#marketOrderTrailingStopLoss').prop('disabled', isDisabled);
        });
        $('#marketOrderTakeProfit').on("change", () => $('#marketOrderTakeProfitInput').prop('disabled', !$('#marketOrderTakeProfit').is(":checked")));

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
            resetMarketOrderForm();

            $('#pendingOrderTab').addClass('disabled');

            fillSymbolsList('#marketOrderSymbolsList');

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
        var id = $(this).attr('id');

        alert('you clicked on button #' + id);
    });

    $(document).on("click", "#createMarketOrderButton", function () {
        resetOrderForms();

        fillSymbolsList('#marketOrderSymbolsList');

        onMarketOrderSymbolChanged();

        $('#orderModalButton').html("Place Order");
        $('#orderModalTitle').html("Create New Order");

        $('#marketOrderTab').tab('show');

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
                break;

            case "modifyPending":
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
        });

        $("#marketOrderVolumeInput").val(symbol.minVolume);
    }

    function fillSymbolsList(listId) {
        var symbolOptions = '';

        $('#symbolsTableBody > tr').each((index, element) => {
            var name = $(element).find('#name').text();

            symbolOptions += `<option value="${name}" id="${element.id}">${name}</option>`;
        });

        $(listId).html(symbolOptions);
    }

    function resetMarketOrderForm() {
        $("#marketOrderSymbolsList").prop('selectedIndex', 0)
        $('#marketOrderSymbolsList').prop('disabled', false);
        $('#marketOrderDirectionList').val('Buy').change();
        $('#marketOrderVolumeInput').val(1).change();
        $('#marketRangeInput').prop('value', 10);
        $('#marketRangeInput').prop('disabled', false);
        $('#marketRange').prop('disabled', false);
        $("#marketOrderStopLoss").prop("checked", false);
        $('#marketOrderStopLossInput').prop('disabled', false);
        $("#marketOrderTakeProfit").prop("checked", false);
        $('#marketOrderTakeProfitInput').prop('disabled', false);
        $('#marketOrderStopLossInput').val(20).change();
        $('#marketOrderTakeProfitInput').val(20).change();
        $('#marketOrderTrailingStopLoss').prop('disabled', false);
        $("#marketOrderTrailingStopLoss").prop("checked", false);
        $('#marketOrderCommenttextarea').val('').change();
        $('#marketOrderCommenttextarea').prop('disabled', false);
    }

    function resetPendingOrderForm() {
    }

    function resetOrderForms() {
        $('#marketOrderTab').removeClass('disabled');
        $('#pendingOrderTab').removeClass('disabled');

        $('#orderModal').data('type', 'createMarket');
        $('#orderModal').data('id', 0);

        resetMarketOrderForm();
        resetPendingOrderForm();
    }
});