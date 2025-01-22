using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL.Calculator
{
    public class TT
    {
        [Key]
        public int TTCode { get; set; }
        public string TTName { get; set; }
    }
}
