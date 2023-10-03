using System;

namespace Portal.Models.MSSQL.Calculator
{
    public class SpecialDay
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DayGroups DayGroups { get; set; }
    }
}
