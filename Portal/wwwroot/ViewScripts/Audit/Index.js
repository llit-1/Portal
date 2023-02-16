// инизиализация страницы, установка размеров блоков
function init() {  
    // параметры главного блока
    var mainPanel = $('#mainPanel');

    mainPanel.css('margin-top', '10px');
    mainPanel.css('padding', '0px');

    var height = $('#global').height();
    height -= $('#nav').outerHeight(true);
    height -= parseInt(mainPanel.css('margin-top'));
    height -= parseInt(mainPanel.css('margin-bottom'));

    mainPanel.css('height', height);

    // параметры scroll-блока
    var header = $('#header');
    var headerHeight = header.height();
    headerHeight += parseInt(header.children('hr').css('margin-bottom'));
    var scrollHeight = height - headerHeight;
    scrollHeight -= parseInt(mainPanel.children('.panel-body').css('padding-top'));
    scrollHeight -= parseInt(mainPanel.children('.panel-body').css('padding-bottom'));

    $('#scroll').css('height', scrollHeight);
}
init();


$(document).ready(function () {
    Alerts();
    $('#loading').hide();    
});

// переинициализация при смене ориентации/изменении размера экрана
$(window).resize(function () {
    init();
});


