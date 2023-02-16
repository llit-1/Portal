using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.TT;
using RKNet_Model.VMS.NX;

namespace Portal.ViewModels
{
    public class TTCamsView
    {
        public List<TT> TTs { get; set; }
        public List<NxSystem> NxSystems {get; set;}
        
        public List<NxCamera> NxCameras { get; set; }
        public TT SelectedTT { get; set; }

        public TTCamsView()
        {
            TTs = new List<TT>();
            NxSystems = new List<NxSystem>();
            NxCameras = new List<NxCamera>();
        }
    }
}
