using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Calculator
{
    public class ReplacementGroups
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int ID { get; set; }

        [Column("Name")]
        public string Name { get; set; }

    }
}
