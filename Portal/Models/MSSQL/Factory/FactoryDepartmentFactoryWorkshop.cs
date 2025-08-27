using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    [Table("FactoryDepartmentFactoryWorkshop")]
    public class FactoryDepartmentFactoryWorkshop
    {
        // int свойства с суффиксом Id, но маппинг на реальные колонки:
        [Column("FactoryDepartment")]
        public int FactoryDepartmentId { get; set; }

        [Column("FactoryWorkshop")]
        public int FactoryWorkshopId { get; set; }

        // навигации — имена такие, как ты используешь в Include/ThenInclude:
        public FactoryDepartment FactoryDepartment { get; set; } = null!;
        public FactoryWorkshop FactoryWorkshop { get; set; } = null!;

        public ICollection<FactoryDepartmentWorkshopJobTitle> DepartmentWorkshopJobTitles { get; set; } = new List<FactoryDepartmentWorkshopJobTitle>();
    }
}