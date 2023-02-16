using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models
{
    public class FOrderXLS
    {
        public string Article { get; set; }
        public string Sku { get; set; } 
        public string MinOrder { get; set; }
        public string MaxOrder { get; set; }
        public string Group { get; set; }
        public string FormingDate { get; set; }
        public string FormingTime { get; set; }
        public string DeliveryDate { get; set; }
    }
}
