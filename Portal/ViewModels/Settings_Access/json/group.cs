using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Settings_Access.json
{
    public class group
    {
        public int id;
        public string name;
        public string description;
        public List<item> roles;
        public List<item> users;

        // для редактора
        public string attribute;
        public List<item> items;
    }
}
