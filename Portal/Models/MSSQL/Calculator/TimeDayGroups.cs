using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL.Calculator
{
    public class TimeDayGroups
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public TimeGroups TimeGroup { get; set; }
        public DayGroups DayGroup { get; set; }

    }
}
