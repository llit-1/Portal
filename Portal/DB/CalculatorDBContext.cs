﻿using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.Calculator;

namespace Portal.DB
{
    public class CalculatorDBContext : DbContext
    {
        public CalculatorDBContext(DbContextOptions<CalculatorDBContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ItemsGroupTimeTT_Coefficient>().HasKey(c => new { c.TTCODE, c.TimeGroupGuid });
            modelBuilder.Entity<ItemsGroupTimeTT_Coefficient>().HasOne(c => c.TimeGroup).WithMany().HasForeignKey(c => c.TimeGroupGuid);
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
        public DbSet<Models.MSSQL.Calculator.NumberOfSales> NumberOfSales { get; set; } // таблица с данными продаж по периодам
        public DbSet<Models.MSSQL.Calculator.SpecialDay> SpecialDays { get; set; } // специальные дни
        public DbSet<Models.MSSQL.Calculator.TT> TT { get; set; } // торговые точки
        public DbSet<Models.MSSQL.Calculator.ReplacementGroups> ReplacementGroups { get; set; } // торговые точки
    }
}
