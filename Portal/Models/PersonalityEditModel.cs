using Microsoft.CodeAnalysis;
using Portal.Models.MSSQL.Personality;
using System.Collections.Generic;

namespace Portal.Models
{
    public class PersonalityEditModel
    {
        public Models.MSSQL.Personality.Personality Personality { get; set; }
        public List<Schedule> Schedules { get; set; }
        public List<JobTitle> JobTitles { get; set; }
        public List<Models.MSSQL.Location.Location> Locations { get; set; }
    }
}
