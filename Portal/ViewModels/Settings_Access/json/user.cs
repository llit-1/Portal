using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Settings_Access.json
{
    public class user
    {
        // аккаунт
        public int id;        
        public string name;
        public string login;
        public string password;
        public string job;
        public string mail;
        public string profitFree;
        public string profitPro;
        
        public List<item> groups;
        public List<item> roles;
        public List<item> objects;

        public bool enabled;
        public bool ad;
        public bool alltt;

        // для редактора
        public string attribute;
        public List<item> items;
    }
}
