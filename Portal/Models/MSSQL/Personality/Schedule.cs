using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Personality
{
    [ComplexType]
    public class Schedule
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public TimeSpan BeginTime { get; set; }
        public TimeSpan EndTime { get; set; }

        [Column("JobTitle")]
        public Guid? JobTitleGuid { get; set; }

        [ForeignKey("JobTitleGuid")]
        public JobTitle? JobTitle { get; set; }

    }
}
