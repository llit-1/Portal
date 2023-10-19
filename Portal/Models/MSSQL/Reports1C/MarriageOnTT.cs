using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Reports1C
{
    [Keyless]
    public class MarriageOnTT
    {
        public DateTime? Date { get; set; }
        public string Article { get; set; }
        public string Nomenclature { get; set; }
        public decimal? Quantity { get; set; }
        public string ReasonMarriage { get; set; }
        public decimal? StorageUnitstatesWeight { get; set; }
        public int CounterpartyCode { get; set; }

    }
}
