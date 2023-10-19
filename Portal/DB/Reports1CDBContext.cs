using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.Reports1C;
using Portal.Models.MSSQL.RK7;

namespace Portal.DB
{
    public class Reports1CDBContext: DbContext
    {
        public Reports1CDBContext(DbContextOptions<Reports1CDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<MarriageOnTT> MarriagesOnTT { get; set; } // справочник товаров

    }
}
