using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactoryPerson")]
    public class FactoryPerson
    {
        [Key, Column("Id")]
        public int Id { get; set; }

        [Required, MaxLength(50), Column("Surname")]
        public string Surname { get; set; } = string.Empty;

        [Required, MaxLength(50), Column("Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50), Column("Patronymic")]
        public string? Patronymic { get; set; }

        [Required, Column("Birthdate", TypeName = "date")]
        public DateTime Birthdate { get; set; }

        [Required, MaxLength(12), Column("INN")]
        public string INN { get; set; } = string.Empty;

        [Required, MaxLength(11), Column("SNILS")]
        public string SNILS { get; set; } = string.Empty;

        // имена свойств совпадают с именами колонок в БД
        [Column("FactoryDepartment")]
        public int FactoryDepartment { get; set; }

        [Column("FactoryWorkshop")]
        public int FactoryWorkshop { get; set; }

        [Column("FactoryJobTitle")]
        public int FactoryJobTitle { get; set; }

        [Column("FactoryCitizenship")]
        public int FactoryCitizenship { get; set; }

        [Column("FactoryEntity")]
        public int FactoryEntity { get; set; }

        [Column("FactoryDocumentType")]
        public int FactoryDocumentType { get; set; }

        [MaxLength(10), Column("Phone")]
        public string? Phone { get; set; }

        [MaxLength(16), Column("CardNumber")]
        public string? CardNumber { get; set; }

        [Column("FactoryBanks")]
        public int? FactoryBanks { get; set; }

        [Column("HostelChekin", TypeName = "date")]
        public DateTime? HostelChekin { get; set; }

        [Column("HostelCheckOut", TypeName = "date")]
        public DateTime? HostelCheckOut { get; set; }

        [Required, Column("HiringDate", TypeName = "date")]
        public DateTime HiringDate { get; set; }

        [Column("DismissedDate", TypeName = "date")]
        public DateTime? DismissedDate { get; set; }

        [Column("Photo")]
        public string? Photo { get; set; }

        [Column("PassCardNumber"), MaxLength(10)]
        public string? PassCardNumber { get; set; }

        // Навигации (простые) — через атрибуты чтобы EF использовал существующие колонки
        [ForeignKey(nameof(FactoryBanks))]
        public FactoryBanks? Bank { get; set; }

        [ForeignKey(nameof(FactoryCitizenship))]
        public FactoryCitizenship? Citizenship { get; set; }

        [ForeignKey(nameof(FactoryEntity))]
        public FactoryEntity? Entity { get; set; }

        [ForeignKey(nameof(FactoryDocumentType))]
        public FactoryDocumentType? DocumentType { get; set; }

        // Навигация на тройную таблицу (композитный FK) — настроим во Fluent API
        public FactoryDepartmentWorkshopJobTitle? DepartmentWorkshopJobTitle { get; set; }
    }
}