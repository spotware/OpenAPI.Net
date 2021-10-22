function isNumberInRange(object) {
    var value = parseFloat(object.value);
    var step = parseFloat(object.step);

    value = value - floatSafeRemainder(value, step);

    object.value = value;

    if (value > parseFloat(object.max))
        object.value = object.max;
    else if (value < parseFloat(object.min))
        object.value = object.min;
}

function floatSafeRemainder(val, step) {
    var valDecCount = (val.toString().split('.')[1] || '').length;
    var stepDecCount = (step.toString().split('.')[1] || '').length;
    var decCount = valDecCount > stepDecCount ? valDecCount : stepDecCount;
    var valInt = parseInt(val.toFixed(decCount).replace('.', ''));
    var stepInt = parseInt(step.toFixed(decCount).replace('.', ''));
    return (valInt % stepInt) / Math.pow(10, decCount);
}

function isNumeric(evt) {
    var theEvent = evt || window.event;
    var key = theEvent.keyCode || theEvent.which;
    key = String.fromCharCode(key);
    var regex = /[0-9]|\./;
    if (!regex.test(key)) {
        theEvent.returnValue = false;
        if (theEvent.preventDefault) theEvent.preventDefault();
    }
}

window.updateSymbolQuote = (quote) => {
    var bid = $('#symbolsTableBody > #' + quote.id + ' > #bid');
    var ask = $('#symbolsTableBody > #' + quote.id + ' > #ask');

    bid.html(quote.bid);
    ask.html(quote.ask);
};

window.showTab = (id) => {
    $(id).tab('show');
};

window.setInputValue = (id, value) => {
    $(id).val(value).change();
};

window.getInputValue = (id) => {
    return $(id).val();
};

window.getNumericInputValue = (id) => {
    return parseFloat($(id).val());
};

window.enableInput = (id) => {
    $(id).prop('disabled', false);
};

window.disableInput = (id) => {
    $(id).prop('disabled', true);
};

window.setNumericInputAttributes = (id, min, max, step) => {
    $(id).attr({
        "max": max,
        "min": min,
        "step": step
    }).change();
};

var chart;

window.createChart = (name, data) => {
    if (chart == null) {
        var ctx = document.getElementById('chartCanvas').getContext('2d');
        ctx.canvas.width = 1000;
        ctx.canvas.height = 250;

        var chartConfig = {
            type: 'candlestick',
            data: {
                datasets: []
            },
            options: {
                responsive: true,
                animation: {
                    duration: 0
                },
                hover: {
                    animationDuration: 0
                },
                responsiveAnimationDuration: 0,
                plugins: {
                    zoom: {
                        pan: {
                            enabled: true,
                            mode: 'xy',
                            overScaleMode: 'y'
                        },
                        zoom: {
                            wheel: {
                                enabled: true,
                            },
                            pinch: {
                                enabled: true,
                            },
                            mode: 'xy',
                            overScaleMode: 'y'
                        }
                    }
                }
            }
        };

        chart = new Chart(ctx, chartConfig);
    }

    var newDataset = {
        label: name,
        fillColor: "rgba(220,220,220,0.2)",
        strokeColor: "rgba(220,220,220,1)",
        pointColor: "rgba(220,220,220,1)",
        pointStrokeColor: "#fff",
        pointHighlightFill: "#fff",
        pointHighlightStroke: "rgba(220,220,220,1)",
        data: data
    };

    if (chart.data.datasets.length > 0) chart.data.datasets.pop();

    chart.data.datasets.push(newDataset);

    chart.update();
};