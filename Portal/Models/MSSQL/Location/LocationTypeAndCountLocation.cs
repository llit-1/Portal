using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Portal.Models.MSSQL.Location
{
    [ComplexType]
    public class LocationTypeAndCountLocation
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public List<Location> Location { get; set; }
        public List<LocationType> LocationType { get; set; }
    }
}
