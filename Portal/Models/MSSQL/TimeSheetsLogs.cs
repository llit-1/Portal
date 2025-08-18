using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class TimeSheetsLogs
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; } 
        public Personality.Personality Personalities { get; set; }
        public Location.Location Location { get; set; }
        public Personality.JobTitle JobTitle { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public string Text { get; set; }
        public TimeSheet? TimeSheets{ get; set; }
    }
}
