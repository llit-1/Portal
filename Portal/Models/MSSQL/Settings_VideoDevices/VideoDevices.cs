using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class VideoDevices
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; } 
        public string Ip { get; set; }
        public Location.Location Location { get; set; }
        public string VideoList { get; set; }
        public int? Status { get; set; }
        public VideoOrientation Orientation { get; set; }
        public int? OnlyMusic { get; set; } // null=video, 1=music, 0=both
        public string Version { get; set; }
    }
}
