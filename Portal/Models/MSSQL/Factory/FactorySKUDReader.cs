using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactorySKUDReader")]
    public class FactorySKUDReader
    {
        public int Id { get; set; }

        // nvarchar(50) NOT NULL
        public string Name { get; set; } = null!;

        // nvarchar(15) NULL
        public string? Ip { get; set; }

        // FOREIGN KEY -> FactorySKUDController.Id (NULLABLE)
        public int? ControllerId { get; set; }

        // int NOT NULL
        public int Relay { get; set; }

        // int NOT NULL
        public int CommandType { get; set; }

        // int NOT NULL
        public int CommandTime { get; set; }

        // nvarchar(50) NULL
        public string? SecurityMonitor { get; set; }

        // Навигация к контроллеру
        public virtual FactorySKUDController? Controller { get; set; }

        // Обратная навигация к логам (из прошлой таблицы)
        public virtual ICollection<FactorySKUDWorkLog> FactorySKUDWorkLogs { get; set; }
            = new HashSet<FactorySKUDWorkLog>();
    }
}
