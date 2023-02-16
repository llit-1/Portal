$('#navTabs').hide();
$('#navBar').hide();

// страницы без горизонтального меню
function loadContent(id, url) {
    $('#navTabs').hide();
    $('#navBar').hide();
    $('#body').removeClass('cm-2-navbar');
    $('#body').addClass('cm-1-navbar');
    SpinnerShow();
    
    var cur = $('#' + id).text();
    $('#' + id).parent().parent().children('li').each(function () {

        if ($(this).children('a').text() != cur) {
            $(this).children('a').removeClass('activeA');
            $(this).children('ul').children('li').children('a').removeClass('activeA');
        }
    });
    $('#' + id).addClass('activeA');
    $(contentContainer).empty();

    // загружаем контент на страницу
    loadUrl(url);
    $('#PageHeader').text($('#' + id).text());
}

// страницы с горизонтальным меню
function loadTabMenu(id, url) {
    $('#navBar').hide();
    $('#navTabs').show();
    $('#body').removeClass('cm-1-navbar');
    $('#body').addClass('cm-2-navbar');

    var cur = $('#' + id).text();
    $('#' + id).parent().parent().children('li').each(function () {

        if ($(this).children('a').text() != cur) {
            $(this).children('a').removeClass('activeA');
            $(this).children('ul').children('li').children('a').removeClass('activeA');
        }
    });
    $('#' + id).addClass('activeA');
    $(contentContainer).empty();

    // загружаем меню подразделов и активируем нажатие на первую вкладку
    $('#navTabs').load(url, function () {        
        $('#navTabs').find('.active').children('a').click();
        $('#PageHeader').text($('#' + id).text());
    });
}

// выделение верхнего раздела
function selectSection(id) {
    //$('#navTabs').hide();
    //$('#navBar').hide();
    //$('#body').removeClass('cm-2-navbar');
    //$('#body').addClass('cm-1-navbar');

    var cur = $('#' + id).text();
    $('#' + id).parent().parent().children('li').each(function () {

        if ($(this).children('a').text() != cur) {
            $(this).children('a').removeClass('activeA');
            $(this).children('ul').children('li').children('a').removeClass('activeA');
        }
    });
    $('#' + id).addClass('activeA');
}
