using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.VMS;
using RKNet_Model.VMS.NX;

namespace Portal.ViewModels
{
    public class CameraView
    {
        public string cameraId { get; set; }
        public string cameraGuid { get; set; }        
        public string camName { get; set; }
        public string sysName { get; set; }
        public string servName { get; set; }
        public string ttName { get; set; }
        public int previewHeight { get; set; }
        public string dateTime { get; set; }
        public VmsTypes vmsType { get; set; }
        public List<Zone> Zones { get; set; }
        public List<NxSystem> NxSystems { get; set; }

        public CameraView()
        {
            NxSystems = new List<NxSystem>();
            Zones = new List<Zone>();
        }
    }
}
