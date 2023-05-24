using Portal.Models.MSSQL.Calculator;
using Portal.ViewModels;
using RKNet_Model.Account;
using System;
using System.Collections.Generic;

namespace Portal.Models.Calculator
{
    public class CalculatorInformation
    {
        public string Name { get; set; }
        public string User { get; set; }
        public int Reaction { get; set; }
        public Guid ItemsGroup { get; set; }
        public string PicturePath { get; set; }
        public TimeDayGroups ThisTimeDayGroup { get; set; }
        public TimeDayGroups NextTimeDayGroup { get; set; }
        public double ThisPeriodCoefficient { get; set; }
        public double NextPeriodCoefficient { get; set; }
        public RKNet_Model.TT.TT TT { get; set; }
        public DateTime Date { get; set; }
        public List<CalculatorItem> Items { get; set; }
    }
}
