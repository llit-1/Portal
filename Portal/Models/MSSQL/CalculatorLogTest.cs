using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;

namespace Portal.Models.MSSQL
{
    public class CalculatorLogTest : CalculatorLog
    {
        public CalculatorLogTest()
        {
            
        }

        public CalculatorLogTest(CalculatorLog calculatorLog)
        {
            UserName = calculatorLog.UserName;
            ItemCode = calculatorLog.ItemCode;
            ItemName = calculatorLog.ItemName;
            TTCode = calculatorLog.TTCode;
            TTName = calculatorLog.TTName;
            Rest = calculatorLog.Rest;
            Result = calculatorLog.Result;
            Fact = calculatorLog.Fact;
            Date = calculatorLog.Date;
            SessionId = calculatorLog.SessionId;
        }
    }
}
