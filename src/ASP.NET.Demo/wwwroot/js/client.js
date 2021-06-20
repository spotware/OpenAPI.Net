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

        onAccountChanged();
    }).catch(function (err) {
        return console.error(err.toString());
    });

    tradingAccountConnection.on("ReceiveSymbols", function (data) {
        var rows = '';
        $.each(data.symbols, function (i, symbol) {
            var item = `<tr id="${symbol.id}">
                            <td>${symbol.name}</td>
                            <td id="bid">${symbol.bid}</td>
                            <td id="ask">${symbol.ask}</td></tr>`;

            rows += item;
        });

        $('#symbols-table-body').html(rows);

        tradingAccountConnection.stream("GetSymbolQuotes", data.accountLogin)
            .subscribe({
                next: (quote) => {
                    var bid = $('#symbols-table-body > #' + quote.id + ' > #bid');
                    var ask = $('#symbols-table-body > #' + quote.id + ' > #ask');

                    bid.html(quote.bid);
                    ask.html(quote.ask);
                },
                complete: () => {
                    console.info("quotes completed");
                },
                error: (err) => {
                    console.error(err.toString());
                },
            });

        isLoaded = true;

        $('#accountLoadingModal').modal('hide');

        event.preventDefault();
    });

    tradingAccountConnection.on("ReceiveSymbolQuote", function (data) {
        $.each($('#symbols-table-body').children('tr'), function (i, row) {
        });
    });

    function onAccountChanged() {
        isLoaded = false;

        $("#accountLoadingModal").modal({
            backdrop: 'static',
            keyboard: false
        });

        $('#accountLoadingModal').modal('toggle')

        var accountLogin = $("#accounts-list").val();

        tradingAccountConnection.invoke("StopSymbolQuotes", accountLogin).catch(function (err) {
            return console.error(err.toString());
        });

        tradingAccountConnection.invoke("GetSymbols", accountLogin).catch(function (err) {
            return console.error(err.toString());
        });

        event.preventDefault();
    };
});