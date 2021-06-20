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
                            <td>${symbol.bid}</td>
                            <td>${symbol.ask}</td></tr>`;

            rows += item;
        });
        $('#symbols-table-body').html(rows);

        isLoaded = true;

        $('#accountLoadingModal').modal('hide');
    });

    function onAccountChanged() {
        isLoaded = false;
        $("#accountLoadingModal").modal({
            backdrop: 'static',
            keyboard: false
        });
        $('#accountLoadingModal').modal('toggle')
        var accountLogin = $("#accounts-list").val();

        tradingAccountConnection.invoke("GetSymbols", accountLogin).catch(function (err) {
            return console.error(err.toString());
        });
        event.preventDefault();
    };
});