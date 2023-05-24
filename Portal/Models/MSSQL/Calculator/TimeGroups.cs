using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL.Calculator
{
    [ComplexType]
    public class TimeGroups
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public int FirstHour { get; set; }
        public int LastHour { get; set; }
        public Guid ItemsGroup { get; set; }
    }
}
