using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL.Personality
{
    [ComplexType]
    public class Personality
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Patronymic { get; set; }
        public DateTime BirthDate { get; set; }
        public JobTitle JobTitle { get; set; }
        public Location.Location Location { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime DismissalsDate { get; set; }
        public Schedule Schedule { get; set; }
        public int Actual { get; set; }
    }
}
