using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Settings_TT
{
    public class CamerasView
    {
        public RKNet_Model.TT.TT SelectedTT;
        public List<RKNet_Model.VMS.NX.NxSystem> NxSystems;
        public List<RKNet_Model.VMS.NX.NxCamera> CamList;
        public List<module_NX.NX.FullInfo.cameraUserAttributes> ServerCams;
        public List<NxLayoutGroup> LayoutGroups;
        public int NxSystemId;
        public CamerasView()
        {
            NxSystems = new List<RKNet_Model.VMS.NX.NxSystem>();
            CamList = new List<RKNet_Model.VMS.NX.NxCamera>();
            ServerCams = new List<module_NX.NX.FullInfo.cameraUserAttributes>();
            LayoutGroups = new List<NxLayoutGroup>();
        }

        public class NxLayoutGroup
        {
            public int SystemId { get; set; }
            public string SystemName { get; set; }
            public string ErrorMessage { get; set; }
            public List<Portal.Global.NxRestClient.NxItemInfo> Layouts { get; set; }

            public NxLayoutGroup()
            {
                Layouts = new List<Portal.Global.NxRestClient.NxItemInfo>();
            }
        }
    }
}

