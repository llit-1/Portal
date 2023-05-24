using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL.Calculator
{
    public class ItemOnTT
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }

        public int TTCode { get; set; }

        public Items Item { get; set; }

        public double Coefficient { get; set; }
    }
}
