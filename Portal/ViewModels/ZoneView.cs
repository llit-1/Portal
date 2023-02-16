using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.VMS;
using RKNet_Model.TT;

namespace Portal.ViewModels
{
    public class ZoneView
    {
        public Zone Zone { get; set; }
        public List<TT> TTs { get; set; }
        public string ttName { get; set; }

        public ZoneView()
        {
            Zone = new Zone();
            TTs = new List<TT>();
        }
    }
}
