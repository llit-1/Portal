using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactoryDepartmentWorkshopJobTitle")]
    public class FactoryDepartmentWorkshopJobTitle
    {
        [Column("FactoryDepartment")]
        public int FactoryDepartmentId { get; set; }

        [Column("FactoryWorkshop")]
        public int FactoryWorkshopId { get; set; }

        [Column("FactoryJobTitle")]
        public int FactoryJobTitleId { get; set; }

        // навигации — ИМЕНА сохранены как ты просил
        public FactoryDepartmentFactoryWorkshop DepartmentWorkshop { get; set; } = null!;
        public FactoryJobTitleFactoryWorkshop JobTitleWorkshop { get; set; } = null!;
    }
}