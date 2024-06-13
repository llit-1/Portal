using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class VideoInfo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; } 
        public string Name { get; set; }
        public string Path { get; set; }
        public int Position { get; set; }
    }
}
