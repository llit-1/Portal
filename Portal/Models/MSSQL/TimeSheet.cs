using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class TimeSheet
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; } 
        public Personality.Personality Personalities { get; set; }
        public Location.Location Location { get; set; }
        public Personality.JobTitle? JobTitle { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public int? Absence { get; set; }
        public decimal? BaseRate { get; set; }
        public decimal? LocationCashBonus { get; set; }
        public decimal? ExperienceCashBonus { get; set; }
        public decimal? PersonalCashBonus { get; set; }
        public decimal? TotalSalary { get; set; }
    }
}
