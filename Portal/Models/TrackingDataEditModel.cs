using Portal.Models.MSSQL.Personality;
using System.Collections.Generic;

namespace Portal.Models
{
    public class TrackingDataEditModel
    {
        public TTData TTData { get; set; }
        public List<Personality> Personalities { get; set; }
        public List<JobTitle> JobTitles { get; set; }
    }
}
