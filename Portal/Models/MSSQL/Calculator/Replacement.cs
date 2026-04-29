using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Calculator
{
    [Table("Replacements")]
    public class Replacement
    {
        [Key]
        public int Id { get; set; }

        public int? ItemCode { get; set; }

        public int? ReplacementItemCode { get; set; }

        public int? TTCode { get; set; }

        public int? ReplacementTTCode { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Begin { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime End { get; set; }
    }
}