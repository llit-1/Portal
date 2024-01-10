using Microsoft.EntityFrameworkCore;
using System;

namespace Portal.Models.MSSQL.Reports1C
{
    [Keyless]
    public class ShipmentByGP
    {
        public DateTime DateOfShipmentChange { get; set; }
        public string Parent { get; set; }
        public string Article { get; set; }
        public string Nomenclature { get; set; }
        public decimal? Quantity { get; set; }
        public string Warehouse { get; set; }
        public decimal? OrderPrice { get; set; }
        public decimal? ConsigneeCodeN { get; set;}

    }
}
