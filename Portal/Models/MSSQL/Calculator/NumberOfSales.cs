using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Calculator
{
    [Keyless]
    public class NumberOfSales
    {
            public Guid ItemOnTTGUID { get; set; }
            public Guid TimeDayGroupsGUID { get; set; }
            public int Hour { get; set; }
            public int Quantity { get; set; }
            public int SettlementDays { get; set; }       
    }
}
