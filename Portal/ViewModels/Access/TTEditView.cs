using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.Account;
using RKNet_Model.TT;
using RKNet_Model.Rk7XML;

namespace Portal.ViewModels.Access
{
    public class TTEditView
    {
        // входные данные
        public TT TT;
        public List<User> Users;
        public List<Group> Groups;

        // выходные данные (для json)
        public int ttId;
        public int[] usersIds;
        public List<CashStation> cashes;

        public TTEditView()
        {
            Users = new List<User>();
            Groups = new List<Group>();
        }
    }  
}
