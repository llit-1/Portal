using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL
{
    public class AIShocase
    {
        [Key]
        public int Id { get; set; }
        public int ZoneId { get; set; }
        public string TT { get; set; }       
        public string ZoneName { get; set; }
        public string DateTime { get; set; }
        public string Value { get; set; }
        public byte[] SourceImage { get; set; }
        public byte[] SecondImage { get; set; }        

    }
}
