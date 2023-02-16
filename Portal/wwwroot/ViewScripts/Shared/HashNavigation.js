// навигиция по якорям страниц (уникальные ссылки + работа кнопки "назад" в браузере и устройствах)
$(function () {
    $(window).hashchange(function () {

        var hash = location.hash;
        //alert(hash);

        // Главная страница
        if (hash == '') loadContent('main', '/Home/Index');                                                 

        // Отчёты
        if (hash == '#reports') loadContent(hash.replace('#', ''), '/Home/Reports');                        // отчёты -> плитка
        if (hash == '#profit') loadContent(hash.replace('#', ''), '/Reports/ProfitFree');                   // отчёты -> выручка
        if (hash == '#profitpro') loadContent(hash.replace('#', ''), '/Reports/ProfitPro');                 // отчёты -> выручка pro
        if (hash == '#profitall') loadContent(hash.replace('#', ''), '/Reports/ProfitAll');                 // отчёты -> выручка all
        if (hash == '#profitplan') loadContent(hash.replace('#', ''), '/Reports/ProfitPlan');               // отчёты -> план продаж по тт
        if (hash == '#sdreport') loadContent(hash.replace('#', ''), '/Reports/ItsmFree');                   // отчёты -> сервис-деск
        if (hash == '#calcusage') loadContent(hash.replace('#', ''), '/Reports/CalcUsage');                 // отчёты -> использование калькуляторов
        if (hash == '#checkstime') loadContent(hash.replace('#', ''), '/Reports/ChecksTime');               // отчёты -> время чеков
        if (hash == '#cashoperations') loadContent(hash.replace('#', ''), '/Reports/CashOperations');       // отчёты -> кассовые опреации
        if (hash == '#Other') loadContent(hash.replace('#', ''), '/Reports/Other');                         // отчёты -> другие отчёты

        // Заказы ТТ
        if (hash == '#ttorders') loadContent(hash.replace('#', ''), '/Home/TTOrders');                         // заказы -> плитка
        if (hash == '#weekorder') loadContent(hash.replace('#', ''), '/ttOrders/OrdersList?orderTypeId=2');    // заказы -> еженедельный заказ
        if (hash == '#monthorder') loadContent(hash.replace('#', ''), '/ttOrders/OrdersList?orderTypeId=3');   // заказы -> ежемесячный заказ
        if (hash == '#franchorders') loadContent(hash.replace('#', ''), '/ttOrders/OrdersList?orderTypeId=1'); // заказы -> регулярные заказы
        if (hash == '#ttorders_prices') loadContent(hash.replace('#', ''), '/ttOrders/OrdersList?orderTypeId=4'); // заказы -> заказ ценников

        // Меню
        if (hash == '#menu') loadContent(hash.replace('#', ''), '/Menu/Index');                             // меню -> плитка

        // Сообщения на кассы
        if (hash == '#cashmsg') loadContent(hash.replace('#', ''), '/CashMessages/Index?finished=0');       // сообщения на кассы -> плитка

        // Стоп-листы продаж
        if (hash == '#skustop') loadContent(hash.replace('#', ''), '/SkuStop/Index?finished=0');            // стоп-листы продаж -> плитка
                
        // Библиотека знаний
        if (hash == '#library') loadContent(hash.replace('#', ''), '/Library/Index');                       // библиотека знаний -> плитка
        if (hash == '#InternalDocs') loadContent(hash.replace('#', ''), '/Library/InternalDocs');           // библиотека знаний -> внутренние документы
        if (hash == '#LudiLove') loadContent(hash.replace('#', ''), '/Library/LudiLove');                   // библиотека знаний -> пекарни люди любят
        if (hash == '#FranchBook') loadContent(hash.replace('#', ''), '/Library/FranchBook');               // библиотека знаний -> справочник франчайзи
        if (hash == '#Passports') loadContent(hash.replace('#', ''), '/Library/Passports');                 // библиотека знаний -> паспорта качества
        if (hash == '#library_oss') loadContent(hash.replace('#', ''), '/Library/Oss');                     // библиотека знаний -> внутренние документы -> отдел системного сопровождения

        // Аудиты
        if (hash == '#AuditIndex') loadContent(hash.replace('#', ''), '/Audit/Index');                      // аудиты -> аудиты пекарен
        if (hash == '#AuditSettings') loadContent(hash.replace('#', ''), '/Audit/Settings');                // аудиты -> настройки аудитов

        // Видеонаблюдение
        if (hash == '#video') loadContent(hash.replace('#', ''), '/Home/Video');                            // видеонаблюдение -> плитка
        if (hash == '#photocam') loadContent(hash.replace('#', ''), '/Video/Photocam');                     // видеонаблюдение -> фотоаналитика

        // Калькуляторы розницы
        if (hash == '#calculators') loadContent(hash.replace('#', ''), '/Home/Calculators');                // калькуляторы -> плитка
        if (hash == '#calc_vipechka') loadContent(hash.replace('#', ''), '/Calculator/Vipechka');           // калькуляторы -> выпечка
        if (hash == '#calc_defrost') loadContent(hash.replace('#', ''), '/Calculator/Conditerka');          // калькуляторы -> кондитерка
        if (hash == '#Sandwitches') loadContent(hash.replace('#', ''), '/Calculator/Sandwitches');          // калькуляторы -> сэндвичи

        // Запросы пользователей
        if (hash == '#userrequests') loadContent(hash.replace('#', ''), '/UserRequests/ReqList?state=new');

        // Логи
        if (hash == '#logs') loadContent(hash.replace('#', ''), '/Log/Index');

        // Настройки
        if (hash == '#settings') loadContent(hash.replace('#', ''), '/Home/Settings');                      // настройки -> плитка
        if (hash == '#SettingsMain') loadContent(hash.replace('#', ''), '/Settings/Main');                  // настройки -> общие
        if (hash == '#SettingsAccess') loadTabMenu(hash.replace('#', ''), '/Settings_Access/TabMenu');      // настройки -> права доступа       
        if (hash == '#SettingsTT') loadTabMenu(hash.replace('#', ''), '/Settings_TT/TabMenu');              // настройки -> торговые точки
        if (hash == '#SettingsVideo') loadTabMenu(hash.replace('#', ''), '/Settings/Video');                // настройки -> видеонаблюдение
        if (hash == '#SettingsRk') loadTabMenu(hash.replace('#', ''), '/Settings/Rk');                      // настройки -> р-кипер
        if (hash == '#SettingsModules') loadContent(hash.replace('#', ''), '/Settings/Modules');            // настройки -> модули

        // Помощь
        if (hash == '#help') loadContent(hash.replace('#', ''), '/Help/Index');                             // помощь

    })
    $(window).hashchange();
});