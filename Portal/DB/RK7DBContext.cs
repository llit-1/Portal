using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.RK7;

namespace Portal.DB
{
    public class RK7DBContext : DbContext
    {

        
        public RK7DBContext(DbContextOptions<RK7DBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<MenuItem> MenuItems { get; set; } // справочник товаров
        public DbSet<Currency> Currencies { get; set; } // справочник валют

    }
}
