using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL
{
    [Keyless]
    public class SaleObject
    {
        public int Midserver { get; set; }
        public int Visit { get; set; }
        public int Code { get; set; }
        [Column(TypeName = "money")]
        public decimal SumWithDiscount { get; set; }
        public double Quantity { get; set; }
        public DateTime Date { get; set; }
        public int Currency { get; set; }
        public int? Restaurant { get; set; }
        public int Deleted { get; set; }
    }
}
