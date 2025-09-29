using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class CashBook
    {
        public int Id { get; set; }
        public string User { get; set; }
        public int? RKCode { get; set; }
        public decimal Cash { get; set; }
        public decimal Incass { get; set; }
        public decimal Other { get; set; }
        public DateTime Date { get; set; }
    }
}
