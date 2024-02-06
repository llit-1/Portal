using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Reports1C
{
    [Keyless]
    public class StrikeItOut
    {
        public DateTime Date { get; set; }
        public string Article { get; set; }
        public decimal? ConsigneeCodeN { get; set; }
        public string WarehouseRecorder { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Amount { get; set; }
        public string ReasonForReturn { get; set; }

    }
}
