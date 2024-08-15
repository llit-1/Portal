using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL
{
    
    public class ReceivedPromocodesVK
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public string UserID { get; set; }
        public int PromocodesVKID { get; set; }
        public DateTime Date { get; set; }
    }
}
