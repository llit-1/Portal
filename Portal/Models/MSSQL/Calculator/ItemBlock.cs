using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Calculator
{
    [Table("ItemBlocks")]
    public class ItemBlock
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemRkCode { get; set; }

        public int? TTCode { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Begin { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime End { get; set; }

        [Required]
        public int Type { get; set; }

        [Required]
        public bool Enable { get; set; }

        public string Info { get; set; }
    }
}
