using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Portal.Models.MSSQL.Calculator;

namespace Portal.Models.Calculator
{
    public class CalculatorItem
    {
        public ItemOnTT ItemOnTT { get; set; }
        public double AverageProductionPeriod { get; set; }
        public double AverageNextPer { get; set; }
        public double AverageSecondNextPer { get; set; }
        public double AverageRestOfThisPeriod { get; set; }

    }

}
