using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace Portal.Models.MSSQL
{
    [ComplexType]
    public class PersonalityLMK
    {
        public int Id { get; set; }
        public int? DocumentTypeId { get; set; }
        public DateTime? MedComissionDate { get; set; }
        public DateTime? FLGDate { get; set; }
        public DateTime? ValidationDate { get; set; }
        public int? VacZonne { get; set; }
        public int? VacGepatit { get; set; }
        public int? VacReject { get; set; }
        public byte[]? FileData { get; set; }
    }

}
