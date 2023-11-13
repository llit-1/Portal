using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using RKNet_Model.TT;
using Portal.Models.MSSQL.Personality;
using RKNet_Model.Account;
using RKNet_Model.Rk7XML;
using RKNet_Model.VMS.NX;

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
        public List<RKNet_Model.Account.User> Users { get; set; }
    } 

}
