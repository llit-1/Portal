using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Calculator
{
    [Keyless]
    public class ItemsGroups
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public int HourForProduction { get; set; }
    }
}
