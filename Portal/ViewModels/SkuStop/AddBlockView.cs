using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.SkuStop
{
    public class AddBlockView
    {
        // входные данные для формы добавления блокировки
        public List<RKNet_Model.TT.TT> TTs;
        public List<MenuItem> MenuItems;

        // выходные данные с формы добавления блокировки
        public string skuName { get; set; }
        public string skuRkCode { get; set; }
        public string reason { get; set; }        
        public string expiration { get; set; }
        public List<int> ttIds { get; set; }
    }
}
