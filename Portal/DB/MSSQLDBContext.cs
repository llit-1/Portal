using Microsoft.EntityFrameworkCore;

namespace Portal.DB
{
    public class MSSQLDBContext : DbContext
    {
        public MSSQLDBContext(DbContextOptions<MSSQLDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Models.MSSQL.AIShocase> AIShocases { get; set; } // таблица с данными заполненности витрин
        public DbSet<Models.MSSQL.SalesPrediction> SalesPredictions { get; set; } // таблица с данными прогноза продаж вручную вводимыми ТМ
        public DbSet<Models.MSSQL.FranchOrder> FranchOrders { get; set; } // заказы ТТ
        public DbSet<Models.MSSQL.PhotoCam> PhotoCams { get; set; } // фото с камер виденаблюдения
        public DbSet<RKNet_Model.MSSQL.Log> Logs { get; set; } // логи действий пользователей
        public DbSet<Models.MSSQL.UserRequest> UserRequests { get; set; } // запросы пользователей
        public DbSet<Models.MSSQL.CashMessage> CashMessages { get; set; } // сообщения на кассы
        public DbSet<RKNet_Model.MSSQL.SkuStop> SkuStops { get; set; } // стоп-листы продаж (блокировка позиций на кассах)

    }
}
