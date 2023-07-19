using System.Collections.Generic;
using System;

namespace Portal.Models
{
    public class TrackingDataTableModel
    {
        public List<Models.MSSQL.Personality.Personality> Personalities { get; set; }

        public List<Models.MSSQL.Personality.Schedule> Schedule { get; set; }

        public List<Models.MSSQL.Location.Location> Location { get; set; }

        public List<RKNet_Model.TT.TT> TTs { get; set; }

        public DateTime BeginDate { get; set; }

        public DateTime EndDate { get; set; }

        public List<TTDateSheets> TTDateSheets { get; set; }
    }

    public class TTDateSheets
        {
            public RKNet_Model.TT.TT TT { get; set; }

            public DateTime Date { get; set; }

            public List<Models.MSSQL.TimeSheet> TimeSheet { get; set; }
        }
}
