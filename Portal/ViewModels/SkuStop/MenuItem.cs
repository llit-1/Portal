using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.SkuStop
{
    public class MenuItem
    {
        public string id;
        public string title;
        public bool isSelectable;
        public List<MenuItem> subs;

        public MenuItem()
        {
            subs = new List<MenuItem>();
        }
    }
}
