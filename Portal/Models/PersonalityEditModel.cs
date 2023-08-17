using Microsoft.CodeAnalysis;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Portal.Models
{
    public class PersonalityEditModel
    {
        public Personality Personality { get; set; }
        public PersonalityVersion PersonalitiesVersions { get; set; }
        public List<Schedule> Schedules { get; set; }
        public List<JobTitle> JobTitles { get; set; }
        public List<MSSQL.Location.Location> Locations { get; set; }
        public List<Entity> Entity { get; set; }
        public string NewPerson { get; set; }
    }
}
