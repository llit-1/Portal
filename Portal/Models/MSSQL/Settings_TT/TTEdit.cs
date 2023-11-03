using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using RKNet_Model.TT;
using Portal.Models.MSSQL.Personality;

namespace Portal.Models.MSSQL.Location

{
    [ComplexType]
    public class TTVersionsEdit
    {
        public List<LocationVersions> LocationVersion { get; set; }
        public List<TT> OldTT { get; set; }
        public List<Entity> Entities { get; set; }
        public List<LocationType> locationTypes { get; set; }
        public bool TTNew { get; set; }
    } 

}
