﻿using Portal.Models.MSSQL.Calculator;
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
        public ItemsGroups ItemsGroup { get; set; }
        public string PicturePath { get; set; }
        public TimeDayGroups ThisTimeDayGroup { get; set; }
        public TimeDayGroups NextTimeDayGroup { get; set; }
        public TimeDayGroups NextSecondTimeDayGroup { get; set; }
        public double ThisPeriodCoefficient { get; set; }
        public double NextPeriodCoefficient { get; set; }
        public double NextSecondPeriodCoefficient { get; set; }
        public List<RKNet_Model.TT.TT> TTs { get; set; }
        public DateTime Date { get; set; }
        public List<CalculatorItem> Items { get; set; }
    }
}
