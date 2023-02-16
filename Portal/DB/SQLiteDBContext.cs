using Microsoft.EntityFrameworkCore;


namespace Portal.DB
{
    public class SQLiteDBContext : DbContext
    {
        public SQLiteDBContext(DbContextOptions<SQLiteDBContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {                        
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<RKNet_Model.Account.User> Users { get; set; } // Таблица пользователей  
        public DbSet<RKNet_Model.Account.Role> Roles { get; set; } // Таблица ролей        
        public DbSet<RKNet_Model.Account.Group> Groups { get; set; } // Таблица групп

        public DbSet<RKNet_Model.TT.TT> TTs { get; set; } // Торговые точки
        public DbSet<RKNet_Model.TT.Type> TTtypes { get; set; } // Типы торговых точек
        public DbSet<RKNet_Model.TT.Organization> Organizations { get; set; } // Юр. лица торговых точек

        public DbSet<RKNet_Model.VMS.NX.NxSystem> NxSystems { get; set; } // Системы NX  
        public DbSet<RKNet_Model.VMS.NX.NxCamera> NxCameras { get; set; } // Камеры NX 
        public DbSet<RKNet_Model.VMS.CamGroup> CamGroups { get; set; } // Группы камер             

        public DbSet<RKNet_Model.VMS.Zone> zones { get; set; } // Зоны для анализа на камерах видеонаблюдения

        public DbSet<Models.Settings.Module> Modules { get; set; } // Управление модулями
        
        public DbSet<RKNet_Model.ttOrders.OrderType> OrderTypes { get; set; } // типы заказов тт
        public DbSet<Models.PortalSettings> PortalSettings { get; set; } // настройки портала

        public DbSet<RKNet_Model.RKSettings> RKSettings { get; set; } // настройки р-кипер
        public DbSet<RKNet_Model.Rk7XML.CashStation> CashStations { get; set; } // кассовые станции р-кипер

        public DbSet<RKNet_Model.Library.RootFolder> RootFolders { get; set; } // корневые каталоги библиотеки знаний
        public DbSet<RKNet_Model.Reports.UserReport> UserReports { get; set; } // ссылки на отчеты Power Bi
        public DbSet<RKNet_Model.Reports.AllReport> AllReports { get; set; } // ссылки на отчеты по всем ТТ

        public DbSet<RKNet_Model.Audit.Item> AuditItems { get; set; } // объекты аудита
        public DbSet<RKNet_Model.Audit.Score> AuditScores { get; set; } // оценки аудита
        
        public DbSet<RKNet_Model.CashClient.ClientVersion> CashClientVersions { get; set; } // версии и файлы кассовых клиентов
        public DbSet<RKNet_Model.CashClient.CashClient> CashClients { get; set; } // состояние касовых клиентов

        public DbSet<RKNet_Model.ApiServerSettings> ApiServerSettings { get; set; } // настройки Api сервера
    }
}
