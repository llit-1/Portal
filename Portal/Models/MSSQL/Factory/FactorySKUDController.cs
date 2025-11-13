using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactorySKUDController")]
    public class FactorySKUDController
    {
        public int Id { get; set; }

        // nvarchar(50) NOT NULL
        public string Name { get; set; } = null!;

        // nvarchar(15) NOT NULL
        public string Ip { get; set; } = null!;

        // int NOT NULL
        public int Pwd { get; set; }

        // Навигация к FactorySKUDReader (1 Controller → много Readers)
        public virtual ICollection<FactorySKUDReader> FactorySKUDReaders { get; set; }
            = new HashSet<FactorySKUDReader>();
    }
}
