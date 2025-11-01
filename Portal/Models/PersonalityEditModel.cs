using DocumentFormat.OpenXml.Office2010.PowerPoint;
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
        public int CurrentPage { get; set; }
        public List<MSSQL.PersonalityDocumentTypes> DocumentTypes { get; set; }
        public MSSQL.PersonalityLMK? PersonalityLMK { get; set; }
        public List<MSSQL.PersonalityCitizenship>? PersonalityCitizenship { get; set; }
    }
}
