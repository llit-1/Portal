using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;

namespace Portal.Models.MSSQL
{
    public class CalculatorLog
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public int ItemCode { get; set; }
        public string ItemName { get; set; }
        public int TTCode { get; set; }
        public string TTName { get; set; }
        public int Rest { get; set; }
        public int Result { get; set; }
        public int Fact { get; set; }
        public DateTime Date { get; set; }
    }
}
