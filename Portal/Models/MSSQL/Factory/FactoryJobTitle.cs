using System.Collections.Generic;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryJobTitle
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<FactoryJobTitleFactoryWorkshop> JobTitleWorkshops { get; set; } = new List<FactoryJobTitleFactoryWorkshop>();
    }
}
