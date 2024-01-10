using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using System.Collections.Generic;

namespace Portal.Models
{
    public class PersonalityModel
    {
        public PersonalityVersion PersonalitiesVersions { get; set; }
        public Personality Personalities { get; set; }
        public List<int> StatusError { get; set; }
        public List<Entity> Entity { get; set; }
        public int maxPage { get; set; }
        public int currentPage { get; set; }
    }
}
