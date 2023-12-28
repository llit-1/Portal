using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL.Personality
{
    [ComplexType]
    public class EntityCost
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public Guid EntityGuid { get; set; }
        public Guid CostEntityGuid { get; set; }
        public string EntityName { get; set; }
        public string CostEntityName { get; set; }
    }
}
