using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Calculator
{
    public class ItemsGroupTimeTT_Coefficient
    {
        public TimeGroups TimeGroup { get; set; }
        public Guid TimeGroupGuid { get; set; }
        public int TTCODE { get; set; }
        public double Coefficient { get; set; }
        public string Name { get; set; }
    }
}
