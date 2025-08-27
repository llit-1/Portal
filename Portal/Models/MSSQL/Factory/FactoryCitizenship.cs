using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryCitizenship
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [Column("CitizenshipType")]
        public int? CitizenshipTypeId { get; set; }   // column name in DB is "CitizenshipType"

        [ForeignKey(nameof(CitizenshipTypeId))]
        public FactoryCitizenshipType? CitizenshipType{ get; set; }
    }
}
