using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL
{
    public class CurrencyType
    {
        [Key]
        public int Rk7CurrencyType { get; set; }
        public int Type { get; set; }
    }
}
