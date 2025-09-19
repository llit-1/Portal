using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
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

        [Authorize(Roles = "cash_book,cashBook_history,cashBook_admin")]
        public async Task<IActionResult> CashBook(int? tt)
        {
            CashBookJson cashBookJson = new();

            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.Include(x => x.TTs).FirstOrDefault(x => x.Login == userLogin);

            List<int> TTs = user.TTs.Select(x => x.Restaurant_Sifr).ToList();

            List<Models.MSSQL.Location.Location> locations = dbSql.Locations.Where(x => TTs.Contains(x.RKCode.Value)).ToList();

            cashBookJson.Locations = locations;

            var cashBooks = dbSql.CashBook
               .Where(x => x.Date >= DateTime.Now.AddDays(-7))
               .Where(x => x.RKCode == tt)
               .OrderByDescending(x => x.Date)
               .ToList();

            if (User.IsInRole("cashBook_admin"))
            {
                cashBooks = dbSql.CashBook
               .Where(x => x.Date >= DateTime.Now.AddDays(-7))
               .OrderByDescending(x => x.Date)
               .ToList();
            }

            if(tt != null)
            {
                cashBookJson.selectedLocation = tt.Value;
            }

            

            cashBookJson.cashBooks = cashBooks;

            return PartialView(cashBookJson);
        }

        public async Task<IActionResult> GetSumSales(int? tt)
        {
            // Получаем cash books
            var cashBooks = await dbSql.CashBook
                .Where(x => x.RKCode == tt)
                .Where(x => x.Date >= DateTime.Now.AddDays(-7))
                .OrderBy(x => x.Date)
                .AsNoTracking()
                .ToListAsync();

            if (!cashBooks.Any())
                return Json(new CashBookJson());

            CashBook? previousCashBook = dbSql.CashBook.Where(x => x.RKCode == tt && x.Date < cashBooks[0].Date)
                                               .OrderByDescending(x => x.Date)
                                               .FirstOrDefault();

            DateTime? minDate = null;

            if (previousCashBook == null)
            {
                minDate = cashBooks.FirstOrDefault().Date;
            } else
            {
                minDate = previousCashBook.Date;
            }

            
            // Определяем диапазон дат для запроса
            var maxDate = cashBooks.Max(x => x.Date);

            // Получаем продажи сразу сгруппированные
            var salesByDate = dbSql.SaleObjects
                .Where(c => c.Restaurant == tt
                         && c.Deleted == 0
                         && c.Date >= minDate
                         && c.Date < maxDate
                         && c.Currency == 1)
                .ToList();

            var data = new List<GetSumSalesJson>();

            

            for (var i = 0; i < cashBooks.Count; i++)
            {
                decimal sum = 0;
                decimal deviation = 0;
                decimal startMoney = 0;
                decimal mustBeMoney = 0;
                List<SaleObject> prev = new List<SaleObject>();

                if (i == 0)
                {
                    prev = salesByDate.Where(x => x.Date < cashBooks[i].Date).ToList();
                }
                else
                {
                    prev = salesByDate.Where(x => x.Date < cashBooks[i].Date && x.Date > cashBooks[i - 1].Date).ToList();
                }

                if (prev != null)
                {
                    sum = prev.Sum(x => x.SumWithDiscount);
                }

                if(previousCashBook != null && i == 0)
                {
                    mustBeMoney = previousCashBook.Cash + sum - cashBooks[i].Incass - cashBooks[i].Other;
                    deviation = cashBooks[i].Cash - mustBeMoney;
                }

                if (i != 0)
                {
                    mustBeMoney = cashBooks[i - 1].Cash + sum - cashBooks[i].Incass - cashBooks[i].Other;
                    deviation = cashBooks[i].Cash - mustBeMoney;
                    startMoney = cashBooks[i-1].Cash;
                }


                data.Add(new GetSumSalesJson
                {
                    mustBeMoney = mustBeMoney,
                    startMoney = startMoney,
                    deviation = deviation,
                    sum = sum,
                    cashBooks = cashBooks[i],
                    tt = dbSql.Locations.FirstOrDefault(x => x.RKCode == tt).Name
                });
            }

            return Json(data);
        }

        public class GetSumSalesJson
        {
            public decimal mustBeMoney { get; set; }
            public decimal startMoney { get; set; }
            public decimal deviation { get; set; }
            public decimal sum { get; set; }
            public CashBook cashBooks { get; set; }
            public string tt { get; set; }
        }

        public class CashBookJson
        {
            public List<Models.MSSQL.Location.Location> Locations { get; set; }
            public List<CashBook>? cashBooks { get; set; }
            public int? selectedLocation { get; set; }
        }

        [Authorize(Roles = "cash_book,cashBook_history,cashBook_admin")]
        public IActionResult CashBookAdd(decimal cash, decimal incass, decimal other, int RKCode)
        {
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.Include(x => x.TTs).FirstOrDefault(x => x.Login == userLogin);

            Models.MSSQL.CashBook cashBook = new();

            cashBook.User = user.Login;
            cashBook.Cash = cash;
            cashBook.Incass = incass;
            cashBook.Other = other;
            cashBook.Date = DateTime.Now;
            cashBook.RKCode = RKCode;

            dbSql.CashBook.Add(cashBook);
            dbSql.SaveChanges();

            return PartialView();
        }
    }
}
