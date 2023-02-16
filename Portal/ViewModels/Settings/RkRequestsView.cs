using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Portal.Models;

namespace Portal.ViewModels.Settings
{
    public class RkRequestsView
    {
        // входные данные
        public List<RKNet_Model.Rk7XML.CashStation> Cashes;
        public string RefIp;

        // выходные данные
        public string ip;
        public string xml_request;        

        public RkRequestsView()
        {           
            Cashes = new List<RKNet_Model.Rk7XML.CashStation>();
        }
    }
}
