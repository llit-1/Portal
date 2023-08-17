using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL.Personality;

namespace Portal.Models.MSSQL.Location
{
    [ComplexType]
    public class LocationVersions
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public Location Location { get; set; }
        public int OBD { get; set; }
        public Entity Entity { get; set; }
        public int Actual { get; set; }
    }
}
