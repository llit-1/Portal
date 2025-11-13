using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    public class FactorySKUDWorkLog
    {

        public int Id { get; set; }

        public string CardNumber { get; set; } = null!;

        public string ReaderIP { get; set; } = null!;

        // FK-свойства + маппинг на реальные имена колонок

        [Column("Reader")]
        public int? ReaderId { get; set; }

        [Column("Person")]
        public int? PersonId { get; set; }

        [Column("ResultType")]
        public int ResultTypeId { get; set; }

        public DateTime DateTime { get; set; }

        public string? HexStr { get; set; }

        // Навигации

        [ForeignKey(nameof(PersonId))]
        public virtual FactoryPerson? Person { get; set; }

        [ForeignKey(nameof(ReaderId))]
        public virtual FactorySKUDReader? Reader { get; set; }

        [ForeignKey(nameof(ResultTypeId))]
        public virtual FactorySKUDResultTypes ResultType { get; set; } = null!;
    }
}