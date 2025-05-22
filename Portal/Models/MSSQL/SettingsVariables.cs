using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Portal.Models.MSSQL
{
    public class SettingsVariables
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Name { get; set; }
        public int Value { get; set; }
        public string? Error { get; set; }
    }
}