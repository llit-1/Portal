using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.TT;

namespace Portal.ViewModels.Settings_TT
{
    public class OrganizationSettings
    {
        public Organization Organization;
        public List<TT> TTs;
        public bool isNew;

        public OrganizationSettings()
        {
            TTs = new List<TT>();
            isNew = false;
        }
    }
}
