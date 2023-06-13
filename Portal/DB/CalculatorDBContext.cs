using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.Calculator;

namespace Portal.DB
{
    public class CalculatorDBContext : DbContext
    {
        public CalculatorDBContext(DbContextOptions<CalculatorDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Models.MSSQL.Calculator.AverageSalesPerHour> AverageSalesPerHour { get; set; } // таблица с данными средних значений
        public DbSet<Models.MSSQL.Calculator.ItemsGroups> ItemsGroups { get; set; } //таблица с группами товаров
        public DbSet<Models.MSSQL.Calculator.Items> Items { get; set; } //продукция
        public DbSet<Models.MSSQL.Calculator.ItemOnTT> ItemOnTT { get; set; } //продукция на точке
        public DbSet<Models.MSSQL.Calculator.TimeGroups> TimeGroups { get; set; } //Группы времени
        public DbSet<Models.MSSQL.Calculator.DayGroups> DayGroups { get; set; } //Группы дней
        public DbSet<Models.MSSQL.Calculator.TimeDayGroups> TimeDayGroups { get; set; } //Группы День-Время
        public DbSet<Models.MSSQL.Calculator.CalculatorReaction> CalculatorReaction { get; set; } //периуд времени разной реакции калькулятора
        public DbSet<Models.MSSQL.Calculator.ItemsGroupTimeTT_Coefficient> ItemsGroupTimeTT_Coefficient { get; set; } //Таблица групповых коэффициентов
    }
}
