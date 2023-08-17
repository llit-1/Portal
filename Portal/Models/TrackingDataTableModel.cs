using Microsoft.CodeAnalysis;
using Portal.Models.MSSQL;
using System;
using System.Collections.Generic;
using Portal.Models.MSSQL.PersonalityVersions;

namespace Portal.Models
{
    public class TrackingDataModel
    {
        public List<TTData> TTDatas { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public List<MSSQL.Location.Location> Location { get; set; } 
    }
    public class TTData
    {
        public Portal.Models.MSSQL.Location.Location Location { get; set; }
        public List<DateData> DateDatas { get; set; }
    }

    public class DateData
    {
        public DateTime Date { get; set; }
        public List<TimeSheet> TimeSheets { get; set; }
    }
}
