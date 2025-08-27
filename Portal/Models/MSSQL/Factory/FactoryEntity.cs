using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portal.Models.MSSQL.Factory
{
    public class FactoryEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
