using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;

namespace Portal.Models.MSSQL
{
    public class FranchOrder
    {
        [Key]
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public string OrderName { get; set; }
        public string TTName { get; set; }
        public int TTCode { get; set; }
        public int TTOBD { get; set; }
        public string Article { get; set; }
        public string SKU { get; set; }
        public int minCount { get; set; }
        public int Count { get; set; }
        public string maxTime { get; set; }
        public DateTime FormingDateTime { get; set; }
        public string minDeliveryDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int? OrderTypeId { get; set; }
        public string OrderTypeName { get; set; }
        public decimal? LastPrice { get; set; }
    }
}
