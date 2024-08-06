    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using System;
    using global::Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL;
using System.Collections.Generic;
using Portal.Models.MSSQL.Location;
using RKNet_Model.Account;

namespace Portal.Models.JsonModels
{
    public class TTsFactoryEditData
    {
        public List<Entity> entity { get; set; }
        public LocationVersions location { get; set; }
        public List<LocationType> locationTypes { get; set; }
        public List<Location> loca { get; set; }
        public List<User> users { get; set; }
        public List<BindingLocationToUsers> pickedusers { get; set; }
    }
}
