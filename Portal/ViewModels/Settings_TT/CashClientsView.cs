using System.Collections.Generic;

namespace Portal.ViewModels.Settings_TT
{
    public class CashClientsView
    {
        public List<RKNet_Model.CashClient.CashClient> Clients { get; set; }
        public List<RKNet_Model.CashClient.ClientVersion> Versions { get; set; }
        public bool isAutoUpdate { get; set; } = false;

        public CashClientsView()
        {
            Clients = new List<RKNet_Model.CashClient.CashClient>();
            Versions = new List<RKNet_Model.CashClient.ClientVersion>();
        }
    }
    
}
