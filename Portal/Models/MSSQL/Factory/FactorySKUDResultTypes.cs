using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactorySKUDResultTypes")]
    public class FactorySKUDResultTypes
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public virtual ICollection<FactorySKUDWorkLog> FactorySKUDWorkLogs { get; set; }
            = new HashSet<FactorySKUDWorkLog>();
    }
}
