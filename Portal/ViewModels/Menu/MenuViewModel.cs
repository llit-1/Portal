using System.Collections.Generic;
using RKNet_Model;
using RKNet_Model.Menu;

namespace Portal.ViewModels.Menu
{
    public class MenuViewModel
    {
        public Category MenuCategory                                    = new Category();
        public List<Category> CategoryPath                              = new List<Category>();
        public Item Item                                                = new Item();
        public List<MeasureUnit> MeasureUnits                           = new List<MeasureUnit>();
        public List<treeMenuItem> rkMenuTree                            = new List<treeMenuItem>();
        public RKNet_Model.Account.User User                            = new RKNet_Model.Account.User();
        public List<RKNet_Model.MSSQL.DeliveryItemStop> DeliveryStops   = new List<RKNet_Model.MSSQL.DeliveryItemStop>();
        public List<RKNet_Model.MSSQL.SkuStop> SkuStops                 = new List<RKNet_Model.MSSQL.SkuStop>();
    }
}
