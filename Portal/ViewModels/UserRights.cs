using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.Account;

namespace Portal.ViewModels
{
    public class UserRights
    {
        public User User;
        public Group Group;
        public List<Role> Roles;
        public List<Group> Groups;
        public List<User> Users;
        public List<RKNet_Model.TT.TT> TTs;
        public bool newItem = false;

        public UserRights()
        {
            Roles = new List<Role>();
            Groups = new List<Group>();
            Users = new List<User>();
            TTs = new List<RKNet_Model.TT.TT>();
        }
        
    }

    public class userJsn
    {
        public int id;
        public string name;
        public string login;
        public string job;
        public string password;
        public string mail;
        public int[] roles;
        public int[] groups;
        public int[] tts;
        public bool aduser;
        public bool alltt;
        public string profitFree;
        public string profitPro;
    }

    public class groupJsn
    {
        public int id;
        public string name;
        public string description;
        public int[] roles;
        public int[] users;
    }
}
