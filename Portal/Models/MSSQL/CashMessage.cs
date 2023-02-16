using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models.MSSQL
{
    public class CashMessage
    {
        public int Id { get; set; }       
        public string Name { get; set; }
        public string Text { get; set; }
        public string UserName { get; set; }
        public int? UserId { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expiration { get; set; }
        public string CashMsgStates { get; set; }
        public string State { get; set; }
        public string Finished { get; set; } 
        public DateTime? FinishedTime { get; set; }

    }    
}
