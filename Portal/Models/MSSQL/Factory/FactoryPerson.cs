using System;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryPerson
    {
        [Key]
        public int Id { get; set; }  // В SQL у тебя тут НЕ identity, поэтому без атрибута Identity

        [Required, MaxLength(50)]
        public string Surname { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Patronymic { get; set; }

        [Required]
        public DateTime Birthdate { get; set; }

        [Required, MaxLength(12)]
        public string INN { get; set; } = string.Empty;

        [Required, MaxLength(14)]
        public string SNILS { get; set; } = string.Empty;

        // !!! Важно: у тебя FactoryDepartment почему-то Identity в таблице,
        // но при этом участвует в составном FK (на FactoryDepartmentWorkshopJobTitle).
        // В EF лучше завести навигации.

        public int FactoryDepartment { get; set; }
        public int FactoryWorkshop { get; set; }
        public int FactoryJobTitle { get; set; }

        public int FactoryCitizenship { get; set; }
        public int FactoryEntity { get; set; }
        public int FactoryDocumentType { get; set; }

        [MaxLength(10)]
        public string? Phone { get; set; }

        [MaxLength(16)]
        public string? CardNumber { get; set; }

        public int? FactoryBanks { get; set; }

        public DateTime? HostelChekin { get; set; }
        public DateTime? HostelCheckOut { get; set; }
        public DateTime HiringDate { get; set; }
        public DateTime? DismissedDate { get; set; }

        // Навигации
        public FactoryBanks? Bank { get; set; }
        public FactoryCitizenship? Citizenship { get; set; }
        public FactoryEntity? Entity { get; set; }
        public FactoryDocumentType? DocumentType { get; set; }

        // Навигация на составной ключ
        public FactoryDepartmentWorkshopJobTitle? DepartmentWorkshopJobTitle { get; set; }
    }
}
