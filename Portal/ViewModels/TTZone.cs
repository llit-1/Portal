using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.VMS;

namespace Portal.ViewModels
{
    public class TTZone
    {
        public string TTName { get; set; }
        public List<Zone> Zones { get; set; }

        public TTZone()
        {
            Zones = new List<Zone>();
        }
    }
}
