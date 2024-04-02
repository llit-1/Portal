using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;

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
        public DbSet<Models.MSSQL.CalculatorLog> CalculatorLogs { get; set; } // логи калькулятора
        public DbSet<Models.MSSQL.CalculatorLogTest> CalculatorLogsTest { get; set; } // тестовые логи калькулятора
        public DbSet<Models.MSSQL.Location.LocationType> LocationTypes { get; set; } // тип локации
        public DbSet<Models.MSSQL.Location.Location> Locations { get; set; } // локация
        public DbSet<Models.MSSQL.Location.LocationVersions> LocationVersions { get; set; } // локация
        public DbSet<Models.MSSQL.Personality.Personality> Personalities { get; set; } // сотрудники
        public DbSet<Models.MSSQL.PersonalityVersions.PersonalityVersion> PersonalityVersions { get; set; } // версии сотрудников
        public DbSet<Models.MSSQL.Personality.JobTitle> JobTitles { get; set; } // должность
        public DbSet<Models.MSSQL.Personality.Schedule> Schedules { get; set; } // типы смен
        public DbSet<Models.MSSQL.TimeSheet> TimeSheets { get; set; } // типы смен
        public DbSet<Models.MSSQL.Personality.Entity> Entity { get; set; } // ЮЛ
        public DbSet<Models.MSSQL.Personality.EntityCost> EntityCost { get; set; } // ЮЛ
        public DbSet<SaleObject> SaleObjects { get; set; }
        public DbSet<SaleObjectsAgregators> SaleObjectsAgregators { get; set; }
        public DbSet<LastUser> LastUser { get; set; }
        public DbSet<CurrencyType> CurrencyTypes { get; set; }
    }
}
