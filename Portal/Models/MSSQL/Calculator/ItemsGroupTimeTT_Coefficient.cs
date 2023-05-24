using Microsoft.EntityFrameworkCore;

namespace Portal.Models.MSSQL.Calculator
{
    [Keyless]
    public class ItemsGroupTimeTT_Coefficient
    {
        public TimeGroups TimeGroup { get; set; }
        public int TTCODE { get; set; }
        public double Coefficient { get; set; }
        public string Name { get; set; }
    }
}
