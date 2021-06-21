$(document).ready(function () {
    var apiErrorsConnection = new signalR.HubConnectionBuilder().withUrl("/apiErrorsHub").build();

    apiErrorsConnection.start().then(function () {
        apiErrorsConnection.stream("GetErrors")
            .subscribe({
                next: (error) => {
                    var toastTemplate = $('#toast-template').contents().clone(true, true);

                    toastTemplate.find('#toast-title').text('Error');
                    toastTemplate.find('#toast-title-small').text(error.type);
                    toastTemplate.find('#toast-icon').addClass('fas fa-exclamation-triangle');
                    toastTemplate.find('.toast-body').text(error.message);

                    var toast = toastTemplate.find(".toast");

                    $('#toasts-container').append(toastTemplate);

                    toast.toast({
                        delay: 100000
                    });

                    $('.toast').toast('show');
                },
                complete: () => {
                    console.info("errors completed");
                },
                error: (err) => {
                    console.error(err.toString());
                },
            });

        event.preventDefault();
    }).catch(function (err) {
        return console.error(err.toString());
    });
});