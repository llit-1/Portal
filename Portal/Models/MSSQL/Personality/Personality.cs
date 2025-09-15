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
        public DateTime BirthDate { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? PhoneCode { get; set; }
        public DateTime? LastPhoneCall { get; set; }
        public int? PhoneCallAttempts { get; set; }
        public string? INN { get; set; }
        public string? SNILS { get; set; }
    }
}
