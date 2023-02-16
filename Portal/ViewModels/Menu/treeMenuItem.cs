using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Menu
{
    public class treeMenuItem
    {
        public string id;
        public string title;
        public bool isSelectable;
        public List<treeMenuItem> subs;
        public string deliveryPrice;

        public treeMenuItem()
        {
            subs = new List<treeMenuItem>();
        }
    }
}
