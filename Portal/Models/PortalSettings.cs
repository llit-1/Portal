using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models
{
    public class PortalSettings
    {
        public int Id { get; set; }
        public int SessionTime { get; set; } // время сессии в минутах, после которого пользователя выбрасывает на страницу авторизации


        public PortalSettings()
        {
            SessionTime = 60;            
        }


    }
}
