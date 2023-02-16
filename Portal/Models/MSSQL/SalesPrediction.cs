using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL
{
    public class SalesPrediction
    {
        [Key]
        public int Id { get; set; }
        public int? TTCode { get; set; }
        public int UserId { get; set; }
        public string TTName { get; set; }
        public string UserName { get; set; }
        public string Date { get; set; }
        public int PredictionValue { get; set; }
    }
}
