using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class CashBook
    {
        public int Id { get; set; }
        public string? User { get; set; }
        public int? TT { get; set; }
        public double? Cash { get; set; }
        public double? Incass { get; set; }
        public double? Other { get; set; }
        public DateTime Date { get; set; }
    }

}
