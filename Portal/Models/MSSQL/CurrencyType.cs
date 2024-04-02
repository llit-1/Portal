using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL
{
    public class CurrencyType
    {
        [Key]
        public int Currency { get; set; }
        public int Type { get; set; }
    }
}
