    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
    using System;
    using global::Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL;
using System.Collections.Generic;
using Portal.Models.MSSQL.Location;

namespace Portal.Models.JsonModels
{
    public class TTsFactoryAdd
    {
        public string guid { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string? code { get; set; }
        public string? type { get; set; }
        public string? entity { get; set; }
        public string? open { get; set; }
        public string? close { get; set; }
        public string? actual { get; set; }
        public string? address { get; set; }
        public string? parent { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }
        public List<string?> usersid { get; set; }
    }
}

