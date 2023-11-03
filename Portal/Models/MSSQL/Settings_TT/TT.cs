using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using RKNet_Model.TT;

namespace Portal.Models.MSSQL.Location

{
    [ComplexType]
    public class TTVersions
    {
        public List<LocationVersions> LocationVersion { get; set; }
        public List<TT> OldTT { get; set; }
    }

}
