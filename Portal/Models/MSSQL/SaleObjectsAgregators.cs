using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL
{
    [Keyless]
    public class SaleObjectsAgregators
    {

        public int Midserver { get; set; }
        public int Code { get; set; }
        [Column(TypeName = "money")]
        public decimal SumWithDiscount { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public string OrderNumber { get; set; }
        public int Currency { get; set; }
        public int? Restaurant { get; set; }
        public int Deleted { get; set; }
    }
}
