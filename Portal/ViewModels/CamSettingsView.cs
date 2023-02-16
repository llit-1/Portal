using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.TT;
using RKNet_Model.VMS;
using RKNet_Model.VMS.NX;

namespace Portal.ViewModels
{
    public class CamSettingsView
    {
        public NxCamera Cam;
        public List<TT> TTs;
        public List<CamGroup> CamGroups;

        public CamSettingsView()
        {
            TTs = new List<TT>();
            CamGroups = new List<CamGroup>();
        }
    }
}
