using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class WorkingSlots
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Personality.Personality Personalities { get; set; }
        public Location.Location Locations { get; set; }
        public Personality.JobTitle JobTitles { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public int Status  { get; set; }
    }
}
