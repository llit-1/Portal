namespace Portal.Models.MSSQL.Factory
{
    public class FactoryDepartmentWorkshopJobTitle
    {
        public int FactoryDepartmentId { get; set; }
        public int FactoryWorkshopId { get; set; }
        public int FactoryJobTitleId { get; set; }

        public FactoryDepartmentFactoryWorkshop DepartmentWorkshop { get; set; } = null!;
        public FactoryJobTitleFactoryWorkshop JobTitleWorkshop { get; set; } = null!;
    }
}
