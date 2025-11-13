using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactorySKUDWorkLog")]
    public class FactorySKUDWorkLog
    {
        public int Id { get; set; }
        public string CardNumber { get; set; } = null!;
        public string ReaderIP { get; set; } = null!;
        public int? ReaderId { get; set; }
        public int? PersonId { get; set; }
        public int ResultTypeId { get; set; }
        public DateTime DateTime { get; set; }

        // nvarchar(50) NULL
        public string? HexStr { get; set; }

        // Навигационные свойства
        public virtual FactoryPerson? Person { get; set; }

        public virtual FactorySKUDReader? Reader { get; set; }

        public virtual FactorySKUDResultTypes ResultType { get; set; } = null!;
    }
}
