using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Settings_Access.json
{
    public class role
    {
        public int id;
        public string name;
        public string code;
        public string level;
        public List<item> users;
        public List<item> groups;

        // для редактора
        public string attribute;
        public List<item> items;
    }
}
