using System.Collections.Generic;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryJobTitleFactoryWorkshop
    {
        public int FactoryJobTitleId { get; set; }
        public int FactoryWorkshopId { get; set; }

        public FactoryJobTitle FactoryJobTitle { get; set; } = null!;
        public FactoryWorkshop FactoryWorkshop { get; set; } = null!;

        public ICollection<FactoryDepartmentWorkshopJobTitle> DepartmentWorkshopJobTitles { get; set; } = new List<FactoryDepartmentWorkshopJobTitle>();
    }
}
