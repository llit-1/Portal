using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Calculator
{
    [Keyless]
    public class AverageSalesPerHour
    {
        public Guid ItemOnTT { get; set; }
        public Guid TimeDayGroups { get; set;}
        public int Hour { get; set; }
        public int Quantity { get; set; }
    }
}
