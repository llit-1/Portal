using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.Account;

namespace Portal.ViewModels.Settings_Access
{
    public class RoleSettings
    {
        public Role Role;
        public List<User> Users;
        public List<Group> Groups;
        public bool newRole;

        public RoleSettings()
        {
            Users = new List<User>();
            Groups = new List<Group>();
            newRole = false;
        }
    }
}
