using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Portal.DB;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Factory;
using RKNet_Model.Account;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static Portal.Controllers.CashBookController;
using static Portal.Controllers.PersonalityFactoryController;
using static RKNet_Model.Rk7XML.Response.GetSystemInfo2Response;


namespace Portal.Controllers
{
    public class CashBookController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        private DB.RK7DBContext rk7Sql;

        public CashBookController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, DB.RK7DBContext rK7DBContext)
        {
            db = context;
            dbSql = dbSqlContext;
            rk7Sql = rK7DBContext;
        }

        [Authorize(Roles = "cash_book")]
        public async Task<IActionResult> CashBook(int? tt)
        {
            CashBookJson cashBookJson = new();

            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.Include(x => x.TTs).FirstOrDefault(x => x.Login == userLogin);

            var TTs = user.TTs;

            cashBookJson.TTs = TTs;

            var cashBooks = dbSql.CashBook
                .OrderByDescending(x => x.Date)
                .ToList();


            cashBookJson.cashBooks = cashBooks;
            //if (!User.IsInRole("cashBook_admin"))
            //{
            //    cashBookJson.cashBooks = dbSql.CashBook
            //    .OrderByDescending(x => x.Date)
            //    .ToList();
            //    List<int> saleCurrenciesTypes = dbSql.CurrencyTypes.Where(c => c.Type == 0).Select(c => c.Rk7CurrencyType).ToList();
            //    List<int> saleCurrencies = rk7Sql.Currencies.Where(c => saleCurrenciesTypes.Contains(c.Parent)).Select(c => c.Sifr).ToList();
            //    var saleObjects = dbSql.SaleObjects.Where(c => (c.Restaurant == tt && c.Deleted == 0 && c.Date >= begin && c.Date < end && saleCurrencies.Contains(c.Currency))).ToList();
            //}
            //else if (User.IsInRole("cashBook_history"))
            //{
            //    var ttIds = TTs.Select(tt => tt.Id).ToList();
            //    cashBookJson.cashBooks = dbSql.CashBook
            //        .Where(x => x.TT != null && ttIds.Contains(x.TT.Value))
            //        .OrderByDescending(x => x.Date)
            //        .ToList();
            //}

            return PartialView(cashBookJson);
        }

        public async Task<IActionResult> GetSumSales(int? tt)
        {
            if (tt == null)
                return BadRequest("TT is required");

            // Получаем код ресторана
            var ttCode = await db.TTs
                .Where(x => x.Id == tt)
                .FirstOrDefaultAsync();


            // Получаем валюты для продаж
            var saleCurrencyTypes = await dbSql.CurrencyTypes
                .Where(c => c.Type == 0)
                .Select(c => c.Rk7CurrencyType)
                .ToListAsync(); // 102, 103

            var saleCurrencies = await rk7Sql.Currencies
                .Where(c => saleCurrencyTypes.Contains(c.Parent))
                .AsNoTracking()
                .Select(c => c.Sifr)
                .ToListAsync();

            // Получаем cash books
            var cashBooks = await dbSql.CashBook
                .Where(x => x.TT == tt)
                .OrderByDescending(x => x.Date)
                .AsNoTracking()
                .ToListAsync();

            if (!cashBooks.Any())
                return Json(new CashBookJson());

            // Определяем диапазон дат для запроса
            var minDate = cashBooks.Min(x => x.Date).Date;
            var maxDate = cashBooks.Max(x => x.Date).Date.AddDays(1);

            // Получаем продажи сразу сгруппированные
            var salesByDate = await dbSql.SaleObjects
                .Where(c => c.Restaurant == ttCode.Restaurant_Sifr
                         && c.Deleted == 0
                         && c.Date >= minDate
                         && c.Date < maxDate
                         && saleCurrencies.Contains(c.Currency))
                .GroupBy(s => s.Date.Date)
                .Select(g => new { Date = g.Key, Sum = g.Sum(s => s.SumWithDiscount) })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Date, x => x.Sum);

            var data = new List<GetSumSalesJson>();

            foreach (var item in cashBooks)
            {
                var dateKey = item.Date.Date;
                var sum = salesByDate.TryGetValue(dateKey, out var total) ? total : 0;

                data.Add(new GetSumSalesJson
                {
                    sum = sum,
                    cashBooks = item,
                    tt = ttCode.Name
                });
            }

            return Json(data);
        }

        public class GetSumSalesJson
        {
            public decimal sum { get; set; }
            public CashBook cashBooks { get; set; }
            public string tt { get; set; }
        }

        public class CashBookJson
        {
            public List<RKNet_Model.TT.TT> TTs { get; set; }
            public List<CashBook>? cashBooks { get; set; }
        }

        [Authorize(Roles = "cash_book")]
        public IActionResult CashBookAdd(double cash, double? incass, double? other, int tt)
        {
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.Include(x => x.TTs).FirstOrDefault(x => x.Login == userLogin);

            if (incass == null && other == null)
            {
                return BadRequest("Некорректные данные");
            }

            Models.MSSQL.CashBook cashBook = new();

            if(incass == null) { incass = 0; }
            if(other == null) { other = 0; }

            cashBook.User = user.Login;
            cashBook.Cash = cash;
            cashBook.Incass = incass;
            cashBook.Other = other;
            cashBook.Date = DateTime.Now;
            cashBook.TT = tt;

            dbSql.CashBook.Add(cashBook);
            dbSql.SaveChanges();

            return PartialView();
        }
    }
}
