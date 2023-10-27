using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;
using Portal.Models.MSSQL.Personality;
using System.Collections.Generic;

namespace Portal.Models.MSSQL.Location
{
    [ComplexType]
    public class EntityLocationModel
    {
        public List<Entity>? Entities { get; set; }
        public List<LocationVersions>? LocationVersions { get; set; }
        public int New { get; set; }
    }
}
