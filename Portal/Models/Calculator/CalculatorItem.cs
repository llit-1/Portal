using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Portal.Models.MSSQL.Calculator;

namespace Portal.Models.Calculator
{
    public class CalculatorItem
    {
        public ItemOnTT ItemOnTT { get; set; }
        public double ProductionPeriodSum { get; set; }
        public double SumRestOfThisPeriod { get; set; }
        public double SumNextPer { get; set; }
        public double SumSecondNextPer { get; set; }
        public double SumProductionPeriod { get; set; }
        public int SettlementDaysRestOfThisPeriod { get; set; }
        public int SettlementDaysNextPer { get; set; }
        public int SettlementDaysSecondNextPer { get; set; }
        public double AverageRestOfThisPeriod { get; set; }
        public double AverageNextPer { get; set; }
        public double AverageSecondNextPer { get; set; }
        public double AverageProductionPeriod { get; set; }
    }

}
