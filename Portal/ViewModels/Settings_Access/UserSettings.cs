using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.TT;
using RKNet_Model.Account;

namespace Portal.ViewModels.Settings_Access
{
    public class UserSettings
    {
        // для редактора
        public User User;
        public List<Role> Roles;
        public List<Group> Groups;
        public List<TT> TTs;
        public bool newUser;    

        public UserSettings()
        {
            Roles = new List<Role>();
            Groups = new List<Group>();
            TTs = new List<TT>();
            newUser = false;
        }
    }
}
