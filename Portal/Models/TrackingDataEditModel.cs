using Portal.Models.MSSQL.Personality;
using System.Collections.Generic;
using Portal.Models.MSSQL.PersonalityVersions;

namespace Portal.Models
{
    public class TrackingDataEditModel
    {
        public TTData TTData { get; set; }
        public List<Personality> Personalities { get; set; }
        public List<JobTitle> JobTitles { get; set; }
        public List<PersonalityVersion> PersonalityVersions { get; set; }
        public double Hours { get; set; }
    }
}
