using System.Collections.Generic;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryWorkshop
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<FactoryDepartmentFactoryWorkshop> DepartmentWorkshops { get; set; } = new List<FactoryDepartmentFactoryWorkshop>();
        public ICollection<FactoryJobTitleFactoryWorkshop> JobTitleWorkshops { get; set; } = new List<FactoryJobTitleFactoryWorkshop>();
    }
}
