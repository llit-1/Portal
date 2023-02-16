using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Settings_Access.json
{
    public class organization
    {
        public int id;
        public string name;
        public string yandexSecret;
        public string yandexClient;
        public string deliveryClubSecret;
        public string deliveryClubClient;
        public List<item> tts;

        // для редактора
        public string attribute;
        public List<item> items;
    }
}
