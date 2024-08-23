using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels
{
    public class FranchOrdersView
    {
        public List<Group> Groups;
        public int orderNumber;
        public List<RKNet_Model.TT.TT> TTs;
        public int orderEditNumber;
        public List<Models.MSSQL.FranchOrder> forders;
        public List<Models.MSSQL.FranchOrder> ThisMonthForders;
        public List<DateTime> deliveryDates;
        public string selectedDate = "current";
        public string selectedTT = "all";
        public string mode = "new";
        public RKNet_Model.ttOrders.OrderType orderType;
        public List<Models.FOrderXLS> items;
        
        public FranchOrdersView()
        {
            Groups = new List<Group>();
            TTs = new List<RKNet_Model.TT.TT>();
            orderEditNumber = 0;
            forders = new List<Models.MSSQL.FranchOrder>();
            deliveryDates = new List<DateTime>();
            items = new List<Models.FOrderXLS>();
        }

        public string defaultName()
        {
            return ("Заказ № " + orderNumber.ToString());
        }

        public class Group
        {
            public string Name;
            public string DeliveryDate;
        }
    }

    
}
