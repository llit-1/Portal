using System.Collections.Generic;
using System.Linq;

namespace Portal.Models.MSSQL.Reports1C
{
    public class ShipmentByGPStrikeItOut : ShipmentByGP
    {

       

        public decimal? StrikeITOutQuantity { get; set; }
        public decimal? StrikeITOutAmount { get; set; }
        public string StrikeITOutReasonForReturn { get; set; }



        public static List<ShipmentByGPStrikeItOut> Collect(List<ShipmentByGP> ShipmentByGP, List<StrikeItOut> StrikeItOut)
        {
            List<ShipmentByGPStrikeItOut> reternList = new();
            foreach (var item in ShipmentByGP)
            {
                ShipmentByGPStrikeItOut shipmentByGPStrikeItOut = new();
                shipmentByGPStrikeItOut.DateOfShipmentChange = item.DateOfShipmentChange;
                shipmentByGPStrikeItOut.Parent = item.Parent;
                shipmentByGPStrikeItOut.Article = item.Article;
                shipmentByGPStrikeItOut.Nomenclature = item.Nomenclature;
                shipmentByGPStrikeItOut.Quantity = item.Quantity;
                shipmentByGPStrikeItOut.Warehouse = item.Warehouse;
                shipmentByGPStrikeItOut.OrderPrice = item.OrderPrice;
                shipmentByGPStrikeItOut.ConsigneeCodeN = item.ConsigneeCodeN;
                StrikeItOut strikeItOut = StrikeItOut.FirstOrDefault(c => c.Date == item.DateOfShipmentChange && c.Article == item.Article && c.ConsigneeCodeN == item.ConsigneeCodeN && c.WarehouseRecorder == item.Warehouse);
                if (strikeItOut != null)
                {
                    shipmentByGPStrikeItOut.StrikeITOutQuantity = strikeItOut.Quantity;
                    shipmentByGPStrikeItOut.StrikeITOutAmount = strikeItOut.Amount;
                    shipmentByGPStrikeItOut.StrikeITOutReasonForReturn = strikeItOut.ReasonForReturn;
                }
                reternList.Add(shipmentByGPStrikeItOut);
            }
            return reternList;
        }



    }
}
