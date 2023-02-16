using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models.MSSQL
{
    // класс для хранения статуса доставки сообщения по каждой кассе (хранится в бд в виде массива json)
    public class CashMsgState
    {
        public int TTId;
        public int? TTCode;
        public string TTName;
        public int cashId;
        public string CashName;

        public bool sended;

        public string error;
    }
}
