using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.TT;
using RKNet_Model.Account;

namespace Portal.ViewModels.Settings_TT
{
    public class TTSettings
    {
        public TT TT;
        public List<RKNet_Model.TT.Type> TTtypes;
        public List<User> Users;
        public List<Organization> Organizations;
        public bool newTT;

        public TTSettings()
        {
            TTtypes = new List<RKNet_Model.TT.Type>();
            Users = new List<User>();
            Organizations = new List<Organization>();
            newTT = false;
        }
    }
}
