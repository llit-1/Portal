using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.TT;

namespace Portal.ViewModels.Settings_TT
{
    public class TTTypeSettings
    {
        public RKNet_Model.TT.Type TTType;
        public List<TT> TTs;
        public bool isNew;

        public TTTypeSettings()
        {
            TTs = new List<TT>();
            isNew = false;
        }
    }
}
