using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL
{

    //  [Table("CalculatorСoefficientLogs", Schema = "dbo")]
    public class CalculatorCoefficientLog
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Указывает, что это первичный ключ
        public int Id { get; set; }
        public double? K1 { get; set; } // Предполагаем, что k1, k2, k3 - числа с плавающей точкой
        public double? K2 { get; set; }
        public double? K3 { get; set; }

        public int? TT { get; set; } // Предполагаем, что TT - строка
        public int? SKU { get; set; } // Предполагаем, что SKU - строка
        public Guid? TimeGroup { get; set; } // Предполагаем, что TimeGroup - строка
        public string Name { get; set; } // Предполагаем, что Name - строка

        public DateTime TaskCreation { get; set; } // Дата и время создания задачи
        public DateTime TaskExecution { get; set; } // Дата и время выполнения задачи

        public string Orderer { get; set; } // Предполагаем, что Orderer - строка
        public int Status { get; set; }
        public string Task { get; set; }
    }

}
