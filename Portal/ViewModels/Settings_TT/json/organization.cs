using Portal.Models.MSSQL.Personality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Settings_Access.json
{
    public class Item
    {
        public string id;
    }

    public class organization
    {
        public string Guid; // Guid Entity
        public string Name; // Location
        public int Owner; // Owner Entity ( 1 - own, 0 - not own)
    }
}
