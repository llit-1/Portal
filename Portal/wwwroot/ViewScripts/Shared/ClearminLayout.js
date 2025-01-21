// определяем контейнер для загрузки контента страниц
var contentContainer = $('#container-fluid');

// спинер - показать
function SpinnerShow() {
    $('#loading').show();
}

// спинер - скрыть
function SpinnerHide() {
    $('#loading').hide();
}

// включение анимации при ajax загрузки страниц
$(document).ajaxStart(function () {
    $('#loading').show();
});
$(document).ajaxComplete(function () {
    $('#loading').hide();
});


// внешний ip пользователя
function GetUserIp() {
    $.getJSON("https://api.ipify.org/?format=json", function (result) {
        $.ajax({
            type: "POST",
            url: '/Home/SetIp',
            data: { userIp: result.ip },
            datatype: "text",
            error: function (error) { alert(error.responseText); }
        });
    });
}
GetUserIp();

// проверка состояния авторизации и  автовыход на страницу входа по истечению куки сессии
function CheckAutorization() {
    //var timer = 60 * 1000; //время в секундах
    //setInterval(function () { UserState(); }, timer);

    //function UserState() {
    //var requestUrl = '/Api/UserState';
    //var loginUrl = '/Account/Login';

    //$.get(requestUrl, function (userState) {
    //if (userState != "True") window.location.replace(loginUrl);
    //});
    //}
}

// выброс пользователя по истечении заданного времени
function AutoLogout() {
    //$.get('/Settings/GetSessionTime', function (time) {
    //time = time * 60 * 1000;
    //setTimeout(function () {
    //window.location.replace('Account/Logout');
    //}, time);
    //});

    $.get('/Api/GetSessionTime', function (time) {
        autoLogout(time);
    });

    function autoLogout(time) {
        var remain = time * 60; // остаток времени в секундах
        
        setInterval(function () {
            $("#time").html(Math.trunc(remain));
            remain = remain - 1;
            if (remain <= 0) {
                window.location.replace('Account/Logout');
            }
        }, 1000);

        setTimeout((x => {
            remain = 0;
        }), remain * 1000)
    }
}
AutoLogout();

// уведомления
function Alerts() {
    $.get('/UserRequests/Alerts', function (reqs) {

        $('#messages').empty();

        var count = 0;
        for (var x in reqs) {
            // показ до 5 уведомелений в списке
            if (count < 5) {

                var name = reqs[x].userName;
                var job = reqs[x].userJobTitle;
                var text = reqs[x].type + ": " + reqs[x].roleName;

                var jobHtml = '<span class="text-small"> (' + job + ')</span>';
                if (!job)
                    jobHtml = "";

                var html = '<a href="#" class="list-group-item jobAlert" onclick="loadContent(\'userrequests\', \'/UserRequests/ReqList\')"><h4 class="list-group-item-heading text-overflow"><i class="fa fa-fw fa-envelope"></i> ' + name + jobHtml + '</h4>' + '<p class="list-group-item-text text-overflow">' + text + '</p></a > ';
                $('#messages').append(html);
            }
            count++;
        }

        if (count > 0)
            $('#alerts_count').text(count);
        else
            $('#alerts_count').text('');
    });    
}

// определение мобильного устройства
function isMobile() {
    if (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent)) {
        return true;
    }
    return false;
}

// кратковременное применение класса на элемент
(function ($) {

    $.fn.extend({

        tempClass: function (className, duration) {
            var elements = this;
            setTimeout(function () {
                elements.removeClass(className);
            }, duration);

            return this.each(function () {
                $(this).addClass(className);
            });
        }
    });

})(jQuery);



// загрузка контента контейнер страницы
function loadUrl(url) {
    $(contentContainer).empty();
    //SpinnerShow();
    $(contentContainer).load(url);
}

// инициализация (выполняется один раз после загрузки шаблона портала)
function init() {
    //устанавливаем параметры контейнера global в зависимости от высоты футера и хедера
    var headerHeight = $('#cm-header').height();
    var footerHeight = $('#cm-footer').height();

    var globalMinHeight = 'calc(100% - ' + footerHeight + 'px)';
    var globalHeight = 'calc(100% - ' + (headerHeight + footerHeight) + 'px)';

    $('#global').css('min-height', globalMinHeight);
    $('#global').css('height', globalHeight);
    $('#global').css('overflow-y', 'auto');

    $('#PageHeader').text('Главная');

    // получаем должность пользователя и устанавливаем под профилем
    $.get('/Account/GetUserInfo', function (user) {
        $('#jobTitle').text(user.jobTitle);
    });

    Alerts();
    $('#spinner').hide();
}

$(document).ready(init());