using Microsoft.EntityFrameworkCore;

namespace Portal.Models.MSSQL.RK7
{
    [Keyless]
    public class Currency
    {
        public int Sifr { get; set; }
        public string Name { get; set; }
    }
}
