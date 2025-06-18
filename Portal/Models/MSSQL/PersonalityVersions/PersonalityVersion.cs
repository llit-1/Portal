using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL.PersonalityVersions
{
    [ComplexType]
    public class PersonalityVersion
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string? Patronymic { get; set; }
        public Personality.JobTitle JobTitle { get; set; }
        public Location.Location Location { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime? DismissalsDate { get; set; }
        public Personality.Schedule? Schedule { get; set; }
        public Personality.Entity? Entity { get; set; }
        public Guid EntityCostGuid { get; set; }
        public DateTime? VersionStartDate { get; set; }
        public DateTime? VersionEndDate { get; set; }
        public Personality.Personality Personalities { get; set; }
        public int Actual { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? PartTimer { get; set; }
    }
}
