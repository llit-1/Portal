using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Factory;
using static Portal.Controllers.StockController;

namespace Portal.DB
{
    public class MSSQLDBContext : DbContext
    {
        public MSSQLDBContext(DbContextOptions<MSSQLDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FactoryDepartmentFactoryWorkshop>()
        .HasKey(x => new { x.FactoryDepartmentId, x.FactoryWorkshopId });

            modelBuilder.Entity<FactoryDepartmentFactoryWorkshop>()
                .HasOne(x => x.FactoryDepartment)
                .WithMany(d => d.DepartmentWorkshops)
                .HasForeignKey(x => x.FactoryDepartmentId);

            modelBuilder.Entity<FactoryDepartmentFactoryWorkshop>()
                .HasOne(x => x.FactoryWorkshop)
                .WithMany(w => w.DepartmentWorkshops)
                .HasForeignKey(x => x.FactoryWorkshopId);

            // --- FactoryJobTitleFactoryWorkshop ---
            modelBuilder.Entity<FactoryJobTitleFactoryWorkshop>()
                .HasKey(x => new { x.FactoryJobTitleId, x.FactoryWorkshopId });

            modelBuilder.Entity<FactoryJobTitleFactoryWorkshop>()
                .HasOne(x => x.FactoryJobTitle)
                .WithMany(j => j.JobTitleWorkshops)
                .HasForeignKey(x => x.FactoryJobTitleId);

            modelBuilder.Entity<FactoryJobTitleFactoryWorkshop>()
                .HasOne(x => x.FactoryWorkshop)
                .WithMany(w => w.JobTitleWorkshops)
                .HasForeignKey(x => x.FactoryWorkshopId);

            // --- FactoryDepartmentWorkshopJobTitle ---
            modelBuilder.Entity<FactoryDepartmentWorkshopJobTitle>()
                .HasKey(x => new { x.FactoryDepartmentId, x.FactoryWorkshopId, x.FactoryJobTitleId });

            modelBuilder.Entity<FactoryDepartmentWorkshopJobTitle>()
                .HasOne(x => x.DepartmentWorkshop)
                .WithMany(dw => dw.DepartmentWorkshopJobTitles)
                .HasForeignKey(x => new { x.FactoryDepartmentId, x.FactoryWorkshopId });

            modelBuilder.Entity<FactoryDepartmentWorkshopJobTitle>()
                .HasOne(x => x.JobTitleWorkshop)
                .WithMany(jw => jw.DepartmentWorkshopJobTitles)
                .HasForeignKey(x => new { x.FactoryJobTitleId, x.FactoryWorkshopId });
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
        public DbSet<Models.MSSQL.CalculatorLogsTest> CalculatorLogsTest { get; set; } // тестовые логи калькулятора
        public DbSet<Models.MSSQL.Location.LocationType> LocationTypes { get; set; } // тип локации
        public DbSet<Models.MSSQL.Location.Location> Locations { get; set; } // локация
        public DbSet<Models.MSSQL.Location.LocationVersions> LocationVersions { get; set; } // локация
        public DbSet<Models.MSSQL.Personality.Personality> Personalities { get; set; } // сотрудники
        public DbSet<Models.MSSQL.PersonalityVersions.PersonalityVersion> PersonalityVersions { get; set; } // версии сотрудников
        public DbSet<Models.MSSQL.Personality.JobTitle> JobTitles { get; set; } // должность
        public DbSet<Models.MSSQL.Personality.Schedule> Schedules { get; set; } // типы смен
        public DbSet<Models.MSSQL.TimeSheet> TimeSheets { get; set; }
        public DbSet<Models.MSSQL.TimeSheetsLogs> TimeSheetsLogs { get; set; }
        public DbSet<Models.MSSQL.Personality.Entity> Entity { get; set; } // ЮЛ
        public DbSet<Models.MSSQL.Personality.EntityCost> EntityCost { get; set; } // ЮЛ
        public DbSet<SaleObject> SaleObjects { get; set; }
        public DbSet<SaleObjectsAgregators> SaleObjectsAgregators { get; set; }
        public DbSet<LastUser> LastUser { get; set; }
        public DbSet<CurrencyType> CurrencyTypes { get; set; }
        public DbSet<Models.MSSQL.VideoDevices> VideoDevices { get; set; } // Информация об устройстве
        public DbSet<Models.MSSQL.VideoInfo> VideoInfo { get; set; } // Информация о видео
        public DbSet<Models.MSSQL.VideoOrientation> VideoOrientation { get; set; } // Информация о видео
        public DbSet<Models.MSSQL.UserSessions> UserSessions { get; set; } // Информация о видео
        public DbSet<Models.MSSQL.BindingLocationToUsers> BindingLocationToUsers { get; set; } // Привязка завода к пользователям
        public DbSet<Models.MSSQL.TimeSheetsFactory> TimeSheetsFactory { get; set; }
        public DbSet<Models.MSSQL.PromocodesVK> PromocodesVK { get; set; }
        public DbSet<Models.MSSQL.ReceivedPromocodesVK> ReceivedPromocodesVK { get; set; }
        public DbSet<Models.MSSQL.SettingsVariables> SettingsVariables { get; set; }
        public DbSet<Models.MSSQL.WorkingSlots> WorkingSlots { get; set; }
        public DbSet<WarehouseCategories> WarehouseCategories { get; set; } // Иерархия склада
        public DbSet<WarehouseHolder> WarehouseHolders  { get; set;}
        public DbSet<BindingPersonalityToLocation> BindingPersonalityToLocation { get; set; }
        public DbSet<CalculatorCoefficientLog> CalculatorСoefficientLogs { get; set; } // изменение коф калькулятора

        public DbSet<FactoryDepartment> FactoryDepartment { get; set; }
        public DbSet<FactoryWorkshop> FactoryWorkshop { get; set; }
        public DbSet<FactoryJobTitle> FactoryJobTitle { get; set; }
        public DbSet<FactoryDepartmentFactoryWorkshop> FactoryDepartmentFactoryWorkshop { get; set; }
        public DbSet<FactoryJobTitleFactoryWorkshop> FactoryJobTitleFactoryWorkshop { get; set; }
        public DbSet<FactoryDepartmentWorkshopJobTitle> FactoryDepartmentWorkshopJobTitle { get; set; }
        public DbSet<FactoryEntity> FactoryEntity { get; set; }
        public DbSet<FactoryDocumentType> FactoryDocumentType { get; set; }
        public DbSet<FactoryCitizenshipType> FactoryCitizenshipType { get; set; }
        public DbSet<FactoryCitizenship> FactoryCitizenship { get; set; }
        public DbSet<FactoryBanks> FactoryBanks { get; set; }
        public DbSet<FactoryPerson> FactoryPerson { get; set; }
    }
}
