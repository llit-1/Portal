// навигиция по якорям страниц (уникальные ссылки + работа кнопки "назад" в браузере и устройствах)
$(function () {
    $(window).hashchange(function () {

        var hash = location.hash;
        //alert(hash);

        // Главная страница
        if (hash == '') loadContent('main', '/Home/Index');                                                 

        // Отчёты
        if (hash == '#reports') loadContent(hash.replace('#', ''), '/Home/Reports');                          // отчёты -> плитка
        if (hash == '#profit') loadContent(hash.replace('#', ''), '/Reports/ProfitFree');                     // отчёты -> выручка
        if (hash == '#profitpro') loadContent(hash.replace('#', ''), '/Reports/ProfitPro');                   // отчёты -> выручка pro
        if (hash == '#profitall') loadContent(hash.replace('#', ''), '/Reports/ProfitAll');                   // отчёты -> выручка all
        if (hash == '#profitplan') loadContent(hash.replace('#', ''), '/Reports/ProfitPlan');                 // отчёты -> план продаж по тт
        if (hash == '#sdreport') loadContent(hash.replace('#', ''), '/Reports/ItsmFree');                     // отчёты -> сервис-деск
        if (hash == '#calcusage') loadContent(hash.replace('#', ''), '/Reports/CalcUsage');                   // отчёты -> использование калькуляторов
        if (hash == '#checkstime') loadContent(hash.replace('#', ''), '/Reports/ChecksTime');                 // отчёты -> время чеков
        if (hash == '#cashoperations') loadContent(hash.replace('#', ''), '/Reports/CashOperations');         // отчёты -> кассовые опреации
        if (hash == '#Other') loadContent(hash.replace('#', ''), '/Reports/Other');                           // отчёты -> другие отчёты
        if (hash == '#GPT') loadContent(hash.replace('#', ''), '/Reports/GPT');                               // отчёты -> чат GPT
        if (hash == '#TemperatureSensors') loadContent(hash.replace('#', ''), '/Reports/TemperatureSensors'); // отчёты -> Датчики температуры СХЗ
        if (hash == '#FranchiseeReports') loadContent(hash.replace('#', ''), '/Reports/FranchiseeReports');   // отчёты -> Отчёты Франчайзи

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
        if (hash == '#Shzhleb') loadContent(hash.replace('#', ''), '/Library/Hleb');                        // библиотека знаний -> сестрорецкий хлебозавод

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
        if (hash == '#calc_bread') loadContent(hash.replace('#', ''), '/Calculator/Calculate?typeGuid=43E2F47F-8729-49C6-8507-64DFEDBC6BBC'); // калькуляторы -> хлеб 2
        if (hash == '#calc_bakery') loadContent(hash.replace('#', ''), '/Calculator/Calculate?typeGuid=573B9B93-41A1-4ACA-B740-A98CEF77E935');  // калькуляторы -> выпечка 2
        if (hash == '#calc_confectionery') loadContent(hash.replace('#', ''), '/Calculator/Calculate?typeGuid=9FA5B2FC-91F1-4771-BF06-F7C3E7E37359');    // калькуляторы -> кондитерка 2
        if (hash == '#Sandwitch') loadContent(hash.replace('#', ''), '/Calculator/Calculate?typeGuid=9C42DDD0-3ABE-4105-AFA1-BFDA989C3836');             // калькуляторы -> сэндвичи 2
        if (hash == '#OtherCalculate') loadContent(hash.replace('#', ''), '/Calculator/Calculate?typeGuid=06BE0412-8D99-4111-99A9-98172E0D3930');             // калькуляторы -> Прочее

        // Учет рабочего времени
        if (hash == '#staff') loadContent(hash.replace('#', ''), '/Home/Staff');
        if (hash == '#Personality') loadContent(hash.replace('#', ''), '/Personality/Personality');
        if (hash == '#TrackingData') loadContent(hash.replace('#', ''), '/TimeTracking/TrackingData');
        

        // Запросы пользователей
        if (hash == '#userrequests') loadContent(hash.replace('#', ''), '/UserRequests/ReqList?state=new');

        // Логи
        if (hash == '#logs') loadContent(hash.replace('#', ''), '/Log/Index');

        // Настройки
        if (hash == '#settings') loadContent(hash.replace('#', ''), '/Home/Settings');                                  // настройки -> плитка
        if (hash == '#SettingsMain') loadContent(hash.replace('#', ''), '/Settings/Main');                              // настройки -> общие
        if (hash == '#SettingsAccess') loadTabMenu(hash.replace('#', ''), '/Settings_Access/TabMenu');                  // настройки -> права доступа    
        if (hash == '#SettingsVideoDevices') loadTabMenu(hash.replace('#', ''), '/Settings_VideoDevices/TabMenu');      // настройки -> видео на ТТ  
        if (hash == '#SettingsTT') loadTabMenu(hash.replace('#', ''), '/Settings_TT/TabMenu');                          // настройки -> торговые точки
        if (hash == '#SettingsVideo') loadTabMenu(hash.replace('#', ''), '/Settings/Video');                            // настройки -> видеонаблюдение
        if (hash == '#SettingsRk') loadTabMenu(hash.replace('#', ''), '/Settings/Rk');                                  // настройки -> р-кипер
        if (hash == '#SettingsModules') loadContent(hash.replace('#', ''), '/Settings/Modules');                        // настройки -> модули

        // Помощь
        if (hash == '#help') loadContent(hash.replace('#', ''), '/Help/Index');                                         // помощь

    })
    $(window).hashchange();
});