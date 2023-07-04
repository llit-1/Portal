using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class TimeSheet
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public Personality.Personality Personality { get; set; }
        public Location.Location Location { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
    }
}
