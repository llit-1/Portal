using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using global::Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL;

namespace Portal.Models.JsonModels
{
    public class TimeSheetJsonModel
    {
        public Guid Location { get; set; }
        public DateTime Date { get; set; }
        public TimeSheetJson[] TimeSheetJson { get; set; }
        public WorkSlotJson[] WorkSlotsJson { get; set; }
    }
    public class TimeSheetJson
    {
        public Guid Personalities { get; set; }
        public Guid Location { get; set; }
        public Guid JobTitle { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
    }

    public class WorkSlotJson
    {
        public int Id { get; set; }
        public Guid JobTitle { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public int Status { get; set; }
    }

}

