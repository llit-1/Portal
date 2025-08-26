using System.Collections.Generic;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryDepartmentFactoryWorkshop
    {
        public int FactoryDepartmentId { get; set; }
        public int FactoryWorkshopId { get; set; }

        public FactoryDepartment FactoryDepartment { get; set; } = null!;
        public FactoryWorkshop FactoryWorkshop { get; set; } = null!;

        public ICollection<FactoryDepartmentWorkshopJobTitle> DepartmentWorkshopJobTitles { get; set; } = new List<FactoryDepartmentWorkshopJobTitle>();
    }
}
