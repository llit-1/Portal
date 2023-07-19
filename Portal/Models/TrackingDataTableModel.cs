<<<<<<< HEAD
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
=======
﻿using Microsoft.CodeAnalysis;
using Portal.Models.MSSQL;
using System;
using System.Collections.Generic;

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
>>>>>>> 698b6dd... Portal v2.14.4.4
