using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models
{
    public class SettingsInternal
    {
        // Версия
        public static string portalVersion = "2.14.5.22";

        // Текущая конфигурация приложения
        public static Microsoft.Extensions.Configuration.IConfiguration Configuration;
        

        // ЗАКАЗЫ ---------------------------------------
        public static class TTOrders
        {
            public static string franchPath = @"\\fs1.shzhleb.ru\LLWork\BI\Франшиза\Заказы франчайзи.xlsx";
            public static string franchList = "Продукция";         
            
            public static string weekPath = @"\\fs1.shzhleb.ru\LLWork\BI\Франшиза\Заказы хозки еженедельный.xlsx";
            public static string weekList = "неделя";

            public static string monthPath = @"\\fs1.shzhleb.ru\LLWork\BI\Франшиза\Заказ хозки ежемесячной.xlsx";
            public static string monthList = "месяц";

            public static string pricesPath = @"\\fs1.shzhleb.ru\LLWork\BI\Франшиза\Заказ ценников.xlsx";
            public static string pricesList = "месяц";
        }
    }
}
