using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class VideoOrientation
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; } 
        public int? Number { get; set; }
        public string Name { get; set; }
        
    }
}
