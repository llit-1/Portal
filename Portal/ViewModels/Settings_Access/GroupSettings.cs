using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.Account;

namespace Portal.ViewModels.Settings_Access
{
    public class GroupSettings
    {
        public Group Group;
        public List<User> Users;
        public List<Role> Roles;
        public bool newGroup;

        public GroupSettings()
        {
            Users = new List<User>();
            Roles = new List<Role>();
            newGroup = false;
        }
    }
}
