using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;



namespace Portal.Models.MSSQL.Location
{
    [ComplexType]
    public class Location
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public LocationType? LocationType { get; set; }
        public int? RKCode { get; set; }
        public int? AggregatorsCode { get; set; }
        public Location? Parent { get; set; }
        public Double? Latitude { get; set; }
        public Double? Longitude { get; set; }
        public int Actual {  get; set; }
    }
}
