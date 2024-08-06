using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Personality;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class TimeSheetsFactory
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; } 
        public PersonalityVersions.PersonalityVersion Personality { get; set; }
        public Location.LocationVersions Location { get; set; }
        public Entity Entity { get; set; }
        public Personality.JobTitle JobTitle { get; set; }
        public int Hours { get; set; }
        public DateTime Date { get; set; }
        public int PartDay { get; set; }
    }
}
