    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using System;
    using global::Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL;
using System.Collections.Generic;
using Portal.Models.MSSQL.Location;

namespace Portal.Models.JsonModels
{
    public class TTsFactoryEdit
    {
        public List<Entity> entity { get; set; }
        public List<LocationVersions> location { get; set; }
        public List<LocationType> locationTypes { get; set; }
    }
}

