using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Portal.ViewModels.Settings_Access.json
{
    public class tt
    {
        public int id;
        public int restaurant_Sifr;
        public int Midserver;
        public string name;
        public string address;
        public string code;
        public string obd;
        public string type;
        public string openDate;
        public string closeDate;
        public string organization;
        public bool closed;
        public bool yandexEda;
        public bool deliveryClub;
        public List<item> users;
        public List<RKNet_Model.Rk7XML.CashStation> cashes;
        public List<Settings_TT.json.ttcams> cameras;

        // для редактора
        public string attribute;
        public List<item> items;

        // БД bdSql
        public string Guid;
        public string Organization;
        public string original;
    }
}
