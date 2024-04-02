using ClosedXML.Excel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using Portal.Models;
using Portal.Models.MSSQL.Reports1C;
using Portal.Models.PowerBi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [Authorize(Roles = "reports")]
    public class ReportsController : Controller
    {
        DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        private DB.RK7DBContext rk7Sql;
        private DB.Reports1CDBContext reports1CSql;

        public ReportsController(DB.SQLiteDBContext sqliteContext, DB.MSSQLDBContext dbSqlContext, DB.RK7DBContext rK7DBContext, DB.Reports1CDBContext reports1CDBContext)
        {
            db = sqliteContext;
            dbSql = dbSqlContext;
            rk7Sql = rK7DBContext;
            reports1CSql = reports1CDBContext;
        }

        // Отчеты в общем доступе (без разграничения на уровне строк в отчетах)
        [Authorize(Roles = "reports_profit")]
        public ActionResult ProfitFree()
        {
            var reportsView = new ViewModels.Reports.ReportsView();

            var login = User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value.ToLower();
            reportsView.User = db.Users.Include(u => u.Reports).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по выручке Free";
            log.Description = "/Reports/ProfitFree";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();
            try
            {
                if (dbSql.LastUser.FirstOrDefault() == null)
                {
                    dbSql.LastUser.Add(new Models.MSSQL.LastUser { User = User.Identity.Name });
                }
                else
                {
                    dbSql.LastUser.FirstOrDefault().User = User.Identity.Name;
                }               
                dbSql.SaveChanges();
            }
            catch (Exception ex)
            {

                throw ex;
            }
           
            return PartialView(reportsView);
        }

        // План продаж
        [Authorize(Roles = "reports_profitplan")]
        public ActionResult ProfitPlan()
        {
            var reportsView = new ViewModels.Reports.ReportsView();

            var login = User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value.ToLower();
            reportsView.User = db.Users.Include(u => u.Reports).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт План продаж по ТТ";
            log.Description = "/Reports/ProfitPlan";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // Использование калькуляторов
        [Authorize(Roles = "reports_calcusage")]
        public IActionResult CalcUsage()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по калькуляторам";
            log.Description = "/Reports/CalcReport";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Время чеков
        [Authorize(Roles = "reports_checkstime")]
        public IActionResult ChecksTime()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "отчёт Время чеков";
            log.Description = "/Reports/CalcReport";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Чат GPT
        //[Authorize(Roles = "reports_calcusage")]
        public IActionResult GPT()
        {
            return PartialView();
        }

        // Датчики температуры
        //[Authorize(Roles = "reports_calcusage")]
        public IActionResult TemperatureSensors()
        {
            return PartialView();
        }

        //Отчёты Франчази
        [Authorize(Roles = "reports_franchisee")]
        public IActionResult FranchiseeReports(string TTcode)
        {
            FranchiseeReportsModel franchiseeReportsModel = new FranchiseeReportsModel();
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs.Where(d => d.CloseDate == null && d.Type != null && d.Type.Id != 3)).FirstOrDefault(c => c.Login == userLogin);
            string UserStr = User.Identity.Name;
            if (String.IsNullOrEmpty(TTcode))
            {
                franchiseeReportsModel.TTs = user.TTs.ToList();
                return PartialView(franchiseeReportsModel);
            }
            else
            {
                franchiseeReportsModel.TTs = user.TTs.Where(c => c.Restaurant_Sifr == int.Parse(TTcode)).ToList();
            }
            if (franchiseeReportsModel.TTs.Count == 0)
            {
                throw new Exception($"В БД отсутствует ТТ с кодом {TTcode}");
            }
            return PartialView(franchiseeReportsModel);
        }


        [Authorize(Roles = "reports_franchisee")]
        public FileContentResult DownloadFranchiseeReport(string Report, string TTcode, string Begin, string End)
        {
            int restaraunt = int.Parse(TTcode);
            DateTime begin = DateTime.ParseExact(Begin, "yyyy-MM-dd", null);
            DateTime end = DateTime.ParseExact(End, "yyyy-MM-dd", null).AddDays(1);
            var menuItems = rk7Sql.MenuItems.ToList();
            var currencies = rk7Sql.Currencies.ToList();
            List<int> saleCurencies = dbSql.CurrencyTypes.Where(c => c.Type == 0).Select(c => c.Currency).ToList();
            switch (Report)
            {
                case "DefectReports":
                    {
                        int obd = db.TTs.FirstOrDefault(c => c.Restaurant_Sifr == restaraunt).Obd;
                        var marriagesOnTT = reports1CSql.MarriagesOnTT.Where(c => c.Date >= begin && c.Date < end && c.CounterpartyCode == obd).ToList();
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Товар";
                            worksheet.Cell(1, 2).Value = "Количество";
                            worksheet.Cell(1, 3).Value = "Причина";
                            worksheet.Cell(1, 4).Value = "Масса";
                            worksheet.Cell(1, 5).Value = "Дата";
                            for (int i = 0; i < marriagesOnTT.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = marriagesOnTT[i].Nomenclature;
                                worksheet.Cell(i + 2, 2).Value = marriagesOnTT[i].Quantity;
                                worksheet.Cell(i + 2, 3).Value = marriagesOnTT[i].ReasonMarriage;
                                worksheet.Cell(i + 2, 4).Value = marriagesOnTT[i].StorageUnitstatesWeight;
                                worksheet.Cell(i + 2, 5).Value = marriagesOnTT[i].Date;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ").xlsx",
                                };
                            }
                        }
                    }

                case "Sales":
                    {
                        var saleObjects = dbSql.SaleObjects.Where(c => (c.Restaurant == restaraunt && c.Deleted == 0 && c.Date >= begin && c.Date < end && saleCurencies.Contains(c.Currency))).ToList();
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Товар";
                            worksheet.Cell(1, 2).Value = "Количество";
                            worksheet.Cell(1, 3).Value = "Цена";
                            worksheet.Cell(1, 4).Value = "Валюта";
                            worksheet.Cell(1, 5).Value = "Дата и время продажи";
                            for (int i = 0; i < saleObjects.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = menuItems.FirstOrDefault(c => c.Code == saleObjects[i].Code).Name;
                                worksheet.Cell(i + 2, 2).Value = saleObjects[i].Quantity;
                                worksheet.Cell(i + 2, 3).Value = saleObjects[i].SumWithDiscount;
                                worksheet.Cell(i + 2, 4).Value = currencies.FirstOrDefault(c => c.Sifr == saleObjects[i].Currency).Name;
                                worksheet.Cell(i + 2, 5).Value = saleObjects[i].Date;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ").xlsx",
                                };
                            }
                        }
                    }
                case "ReternsOff":
                    {
                        var saleObjects = dbSql.SaleObjects.Where(c => (c.Restaurant == restaraunt && c.Deleted == 0 && c.Date >= begin && c.Date < end && (c.Currency == 1003299 || c.Currency == 1000716))).ToList();
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Товар";
                            worksheet.Cell(1, 2).Value = "Количество";
                            worksheet.Cell(1, 3).Value = "Цена";
                            worksheet.Cell(1, 4).Value = "Дата и время возврата";
                            for (int i = 0; i < saleObjects.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = menuItems.FirstOrDefault(c => c.Code == saleObjects[i].Code).Name;
                                worksheet.Cell(i + 2, 2).Value = saleObjects[i].Quantity;
                                worksheet.Cell(i + 2, 3).Value = saleObjects[i].SumWithDiscount;
                                worksheet.Cell(i + 2, 4).Value = saleObjects[i].Date;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ").xlsx",
                                };
                            }
                        }
                    }

                case "WriteOut":
                    {
                        var saleObjects = dbSql.SaleObjects.Where(c => (c.Restaurant == restaraunt && c.Deleted == 0 && c.Date >= begin && c.Date < end && c.Currency != 1003299 && c.Currency != 1000716 && c.Currency != 1000721 && !saleCurencies.Contains(c.Currency))).ToList();
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Товар";
                            worksheet.Cell(1, 2).Value = "Количество";
                            worksheet.Cell(1, 3).Value = "Цена";
                            worksheet.Cell(1, 4).Value = "Тип списания";
                            worksheet.Cell(1, 5).Value = "Дата и время списания";
                            for (int i = 0; i < saleObjects.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = menuItems.FirstOrDefault(c => c.Code == saleObjects[i].Code).Name;
                                worksheet.Cell(i + 2, 2).Value = saleObjects[i].Quantity;
                                worksheet.Cell(i + 2, 3).Value = saleObjects[i].SumWithDiscount;
                                worksheet.Cell(i + 2, 4).Value = currencies.FirstOrDefault(c => c.Sifr == saleObjects[i].Currency).Name;
                                worksheet.Cell(i + 2, 5).Value = saleObjects[i].Date;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ").xlsx",
                                };
                            }
                        }
                    }
                case "AgregatorSales":
                    {
                        var saleObjectsAgregators = dbSql.SaleObjectsAgregators.Where(c => (c.Restaurant == restaraunt && c.Deleted == 0 && c.Date >= begin && c.Date < end)).ToList();
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Товар";
                            worksheet.Cell(1, 2).Value = "Количество";
                            worksheet.Cell(1, 3).Value = "Цена";
                            worksheet.Cell(1, 4).Value = "Агрегатор";
                            worksheet.Cell(1, 5).Value = "Номер Заказа";
                            worksheet.Cell(1, 6).Value = "Дата и время продажи";
                            for (int i = 0; i < saleObjectsAgregators.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = menuItems.FirstOrDefault(c => c.Code == saleObjectsAgregators[i].Code).Name;
                                worksheet.Cell(i + 2, 2).Value = saleObjectsAgregators[i].Quantity;
                                worksheet.Cell(i + 2, 3).Value = saleObjectsAgregators[i].SumWithDiscount;
                                worksheet.Cell(i + 2, 4).Value = currencies.FirstOrDefault(c => c.Sifr == saleObjectsAgregators[i].Currency).Name;
                                worksheet.Cell(i + 2, 5).Value = saleObjectsAgregators[i].OrderNumber;
                                worksheet.Cell(i + 2, 6).Value = saleObjectsAgregators[i].Date;
                            }
                             using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ").xlsx",
                                };
                            }
                        }
                    }

                case "Inventarization":
                    {
                        var saleObjects = dbSql.SaleObjects.Where(c => (c.Restaurant == restaraunt && c.Deleted == 0 && c.Date >= begin && c.Date < end && c.Currency == 1000721)).ToList();
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Товар";
                            worksheet.Cell(1, 2).Value = "Количество";
                            worksheet.Cell(1, 3).Value = "Цена";
                            worksheet.Cell(1, 4).Value = "Тип списания";
                            worksheet.Cell(1, 5).Value = "Дата и время списания";
                            for (int i = 0; i < saleObjects.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = menuItems.FirstOrDefault(c => c.Code == saleObjects[i].Code).Name;
                                worksheet.Cell(i + 2, 2).Value = saleObjects[i].Quantity;
                                worksheet.Cell(i + 2, 3).Value = saleObjects[i].SumWithDiscount;
                                worksheet.Cell(i + 2, 4).Value = currencies.FirstOrDefault(c => c.Sifr == saleObjects[i].Currency).Name;
                                worksheet.Cell(i + 2, 5).Value = saleObjects[i].Date;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ".xlsx",
                                };
                            }
                        }
                    }


                case "Shipment":
                    {
                        int obd = db.TTs.FirstOrDefault(c => c.Restaurant_Sifr == restaraunt).Obd;
                        var shipmentsByGP = reports1CSql.ShipmentsByGP.Where(c => c.DateOfShipmentChange >= begin && c.DateOfShipmentChange < end && c.ConsigneeCodeN == obd).ToList();
                        var strikeItOut = reports1CSql.StrikeItOut.Where(c => c.Date >= begin && c.Date < end && c.ConsigneeCodeN == obd).ToList();
                        var shipmentsByGPStrikeItOut = ShipmentByGPStrikeItOut.Collect(shipmentsByGP, strikeItOut);
                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Родитель";
                            worksheet.Cell(1, 2).Value = "Артикул";
                            worksheet.Cell(1, 3).Value = "Номенклатура";
                            worksheet.Cell(1, 4).Value = "Количество";
                            worksheet.Cell(1, 5).Value = "Склад";
                            worksheet.Cell(1, 6).Value = "Цена заказа";
                            worksheet.Cell(1, 7).Value = "Дата";
                            worksheet.Cell(1, 8).Value = "Количество вычерка";
                            worksheet.Cell(1, 9).Value = "Цена вычерка";
                            worksheet.Cell(1, 10).Value = "Причина вычерка";
                            for (int i = 0; i < shipmentsByGPStrikeItOut.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = shipmentsByGPStrikeItOut[i].Parent;
                                worksheet.Cell(i + 2, 2).Value = shipmentsByGPStrikeItOut[i].Article;
                                worksheet.Cell(i + 2, 3).Value = shipmentsByGPStrikeItOut[i].Nomenclature;
                                worksheet.Cell(i + 2, 4).Value = shipmentsByGPStrikeItOut[i].Quantity;
                                worksheet.Cell(i + 2, 5).Value = shipmentsByGPStrikeItOut[i].Warehouse;
                                worksheet.Cell(i + 2, 6).Value = shipmentsByGPStrikeItOut[i].OrderPrice;
                                worksheet.Cell(i + 2, 7).Value = shipmentsByGPStrikeItOut[i].DateOfShipmentChange;
                                worksheet.Cell(i + 2, 8).Value = shipmentsByGPStrikeItOut[i].StrikeITOutQuantity;
                                worksheet.Cell(i + 2, 9).Value = shipmentsByGPStrikeItOut[i].StrikeITOutAmount;
                                worksheet.Cell(i + 2, 10).Value = shipmentsByGPStrikeItOut[i].StrikeITOutReasonForReturn;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + Begin + "_" + End + ").xlsx",
                                };
                            }
                        }
                    }
                case "Prices":
                    {
                        int obd = db.TTs.FirstOrDefault(c => c.Restaurant_Sifr == restaraunt).Obd;
                        var shipmentsByGP = reports1CSql.ShipmentsByGP.Where(c => c.DateOfShipmentChange >= DateTime.Now.AddDays(-30) && c.ConsigneeCodeN == obd).ToList();
                        shipmentsByGP = shipmentsByGP.GroupBy(c => new {c.Article, c.Nomenclature}).Select(i => i.OrderByDescending(c => c.DateOfShipmentChange).First()).ToList();

                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("Лист 1");
                            worksheet.Cell(1, 1).Value = "Артикул";
                            worksheet.Cell(1, 2).Value = "Номенклатура";
                            worksheet.Cell(1, 3).Value = "Цена";
                            worksheet.Cell(1, 4).Value = "Дата";
                            for (int i = 0; i < shipmentsByGP.Count; i++)
                            {
                                worksheet.Cell(i + 2, 1).Value = shipmentsByGP[i].Article;
                                worksheet.Cell(i + 2, 2).Value = shipmentsByGP[i].Nomenclature;
                                worksheet.Cell(i + 2, 3).Value = shipmentsByGP[i].OrderPrice / shipmentsByGP[i].Quantity;
                                worksheet.Cell(i + 2, 4).Value = shipmentsByGP[i].DateOfShipmentChange;
                            }

                            using (var stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);
                                return new FileContentResult(stream.ToArray(), "application/zip")
                                {
                                    FileDownloadName = Report + "(" + DateTime.Now + ").xlsx",
                                };
                            }
                        }

                    }


                default:
                    return null;
            }



        }


        // Кассовые операции
        [Authorize(Roles = "reports_cashoperations")]
        public IActionResult CashOperations()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "отчёт Кассовые опреации";
            log.Description = "/Reports/CashOperations";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public ActionResult ItsmFree()
        {
            var reportsView = new ViewModels.Reports.ReportsView();
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт ITSM Free";
            log.Description = "/Reports/ItsmFree";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // Про версии отчетов, требующие авторизации Microsoft Azure
        [Authorize(Roles = "reports_pro")]
        public ActionResult ProfitPro()
        {
            var reportsView = new ViewModels.Reports.ReportsView();

            var login = User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value.ToLower();
            reportsView.User = db.Users.Include(u => u.Reports).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по выручке Pro";
            log.Description = "/Reports/ProfitPro";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // Спец. версия отчета со служебными валютами
        [Authorize(Roles = "reports_special")]
        public ActionResult ProfitAll()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по выручке All";
            log.Description = "/Reports/ProfitAll";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Другие отчёты
        [Authorize(Roles = "reports_other")]
        public ActionResult Other()
        {
            var rootPath = db.RootFolders.FirstOrDefault(r => r.Id == 13).Path;

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Другие отчёты";
            log.Description = "/Reports/Other";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return RedirectToAction("Folder", new { path = rootPath });
        }

        // Папка
        public IActionResult Folder(string path)
        {
            if (path == "Reports")
            {
                return Redirect("/Home/Reports");
            }

            var folderView = new ViewModels.Library.FolderView();

            // разэкранирование "плюс" и "пробел"
            path = path.Replace("plustoreplace", "+");
            path = path.Replace("backspacetoreplace", " ");

            // текущий каталог
            folderView.curDirectory = new DirectoryInfo(path);

            // корневой каталог раздела
            var rootItem = db.RootFolders.FirstOrDefault(r => path.Contains(r.Path));
            folderView.navItems.Add(rootItem);
            var rootDir = new DirectoryInfo(rootItem.Path);

            // промежуточные каталоги
            var curPath = folderView.curDirectory.FullName.Replace(rootDir.FullName, "");
            string[] dirs = curPath.Split('\\');

            var tempPath = rootDir.FullName;
            foreach (var dir in dirs)
            {
                if (dir.Length > 0)
                {
                    tempPath += "\\" + dir;
                    var item = new RKNet_Model.Library.RootFolder();
                    item.Name = dir;
                    item.Path = tempPath;
                    folderView.navItems.Add(item);
                }
            }

            // назад
            var itemsCount = folderView.navItems.Count;
            if (itemsCount > 1)
            {
                folderView.prevPath = folderView.navItems[itemsCount - 2].Path;
            }
            else
            {
                folderView.prevPath = "Reports";
            }

            return PartialView("Other", folderView);
        }

        // Встраиваемые отчёты Embedded
        public async Task<IActionResult> ProfitEmbedded()
        {
            return RedirectToAction("Unavailable", "Home");

            // Параметры подключения к зарегистрированному приложению Power Bi
            PBIAppIntegration pbiApp = new PBIAppIntegration
            {
                ApiUrl = "https://api.powerbi.com/",
                ApplicationId = new Guid("947db2de-3e35-45ae-8af0-c2313c3218dd"),
                ApplicationSecret = "2xOYw_Tj9~S6u.a2W6J~JzK6D.~5ojKe.7",
                AuthorityUrl = "https://login.microsoftonline.com/b21b8975-765a-4273-8352-5c758cebf791/oauth2/token",
                EmbedUrlBase = "https://app.powerbi.com/",
                ResourceUrl = "https://analysis.windows.net/powerbi/api",
                UserName = "powerbi@shzhleb.ru",
                Password = "'ythubz-'ythubz=59"
            };

            // Парметры отчета Power Bi
            Models.PowerBi.Report report = new Models.PowerBi.Report
            {
                WorkspaceId = new Guid("20ad784f-9d5a-4988-9660-d1b9c3054841"),
                DataSet = "b63c39b9-b629-4484-921c-5ffc82ea20ef",
                ReportId = new Guid("cccaaf7e-0654-4afa-9e3f-4dd0bd982f17")
            };

            // роли Power Bi
            List<string> pbiRoles = new List<string>();

            // присваиваем все роли пользователя ролям Power Bi
            foreach (var claim in User.Claims)
            {
                if (claim.Type == System.Security.Claims.ClaimsIdentity.DefaultRoleClaimType) pbiRoles.Add(claim.Value);
            }

            // выбираем версию отчета в зависимости от роли
            var firstRole = pbiRoles.FirstOrDefault();

            switch (firstRole)
            {
                case "typeRoleHere":
                    report.ReportId = new Guid("6dc259e6-80b3-4470-bee8-ca2c5e5c8bf7");
                    break;
                default:
                    break;
            }

            var reportConfig = await GetPBIEmbedConfig(pbiApp, report, pbiRoles);

            return PartialView(reportConfig);
        }

        public async Task<IActionResult> ItsmEmbedded()
        {
            return RedirectToAction("Unavailable", "Home");

            // Параметры подключения к зарегистрированному приложению Power Bi
            PBIAppIntegration pbiApp = new PBIAppIntegration
            {
                ApiUrl = "https://api.powerbi.com/",
                ApplicationId = new Guid("947db2de-3e35-45ae-8af0-c2313c3218dd"),
                ApplicationSecret = "2xOYw_Tj9~S6u.a2W6J~JzK6D.~5ojKe.7",
                AuthorityUrl = "https://login.microsoftonline.com/b21b8975-765a-4273-8352-5c758cebf791/oauth2/token",
                EmbedUrlBase = "https://app.powerbi.com/",
                ResourceUrl = "https://analysis.windows.net/powerbi/api",
                UserName = "powerbi@shzhleb.ru",
                Password = "'ythubz-'ythubz=59"
            };

            // Парметры отчета Power Bi
            Models.PowerBi.Report report = new Models.PowerBi.Report
            {
                WorkspaceId = new Guid("20ad784f-9d5a-4988-9660-d1b9c3054841"),
                ReportId = new Guid("31bbcc07-4ea5-420d-811e-2c07ec4b3717"),
                DataSet = "cc9f6a94-e221-42a4-a610-94771b8c3705"
            };

            // роли Power Bi
            List<string> pbiRoles = new List<string>();

            // присваиваем все роли пользователя ролям Power Bi
            //foreach (var claim in User.Claims)
            //{
            //if (claim.Type == System.Security.Claims.ClaimsIdentity.DefaultRoleClaimType) pbiRoles.Add(claim.Value);
            //}

            // Присваеваем роль all всем пользователям
            pbiRoles.Add("all");

            var reportConfig = await GetPBIEmbedConfig(pbiApp, report, pbiRoles);

            return PartialView(reportConfig);
        }

        // Отчёты в разработке
        public async Task<IActionResult> WorkReports()
        {
            // Параметры подключения к зарегистрированному приложению Power Bi
            PBIAppIntegration pbiApp = new PBIAppIntegration
            {
                ApiUrl = "https://api.powerbi.com/",
                ApplicationId = new Guid("947db2de-3e35-45ae-8af0-c2313c3218dd"),
                ApplicationSecret = "2xOYw_Tj9~S6u.a2W6J~JzK6D.~5ojKe.7",
                AuthorityUrl = "https://login.microsoftonline.com/b21b8975-765a-4273-8352-5c758cebf791/oauth2/token",
                EmbedUrlBase = "https://app.powerbi.com/",
                ResourceUrl = "https://analysis.windows.net/powerbi/api",
                UserName = "powerbi@shzhleb.ru",
                Password = "'ythubz-'ythubz=59"
            };

            // Парметры отчета Power Bi
            Models.PowerBi.Report report = new Models.PowerBi.Report
            {
                WorkspaceId = new Guid("1356f721-8012-421e-8dd6-5fff62fbdc89"),
                DataSet = "353e6db1-e56e-4ced-9da6-59afc19fe39b",
                ReportId = new Guid("bb149fcf-a8fb-4fce-8e92-d5ef93c7219d")
            };

            // роли Power Bi
            List<string> pbiRoles = new List<string>();

            // присваиваем все роли пользователя ролям Power Bi
            foreach (var claim in User.Claims)
            {
                if (claim.Type == System.Security.Claims.ClaimsIdentity.DefaultRoleClaimType) pbiRoles.Add(claim.Value);
            }

            var reportConfig = await GetPBIEmbedConfig(pbiApp, report, pbiRoles);

            return PartialView(reportConfig);
        }

        //служебные методы-----------------------------------------------------------------------------------------------------------

        // Запрос отчёта PowerBi
        private async Task<PowerBIEmbedConfig> GetPBIEmbedConfig(PBIAppIntegration pbiApp, Models.PowerBi.Report report, List<string> pbiRoles)
        {
            var result = new PowerBIEmbedConfig { Username = pbiApp.UserName };
            var accessToken = await GetPowerBIAccessToken(pbiApp);
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");

            using (var client = new PowerBIClient(new Uri(pbiApp.ApiUrl), tokenCredentials))
            {
                var workspaceId = report.WorkspaceId;
                var reportId = report.ReportId;
                var reportData = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);

                // токен без ролей Power Bi
                //var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                //var tokenResponse = await client.Reports.GenerateTokenAsync(workspaceId, reportId, generateTokenRequestParameters);


                // токен с ролями Power Bi
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "View", null, identities: new List<EffectiveIdentity> { new EffectiveIdentity(username: pbiApp.UserName, roles: pbiRoles, datasets: new List<string> { report.DataSet }) });
                var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(workspaceId, reportId, generateTokenRequestParameters);

                result.EmbedToken = tokenResponse;
                result.EmbedUrl = reportData.EmbedUrl;
                result.Id = reportData.Id.ToString();
            }

            return result;
        }

        // Получение токена PowerBi
        private async Task<string> GetPowerBIAccessToken(PBIAppIntegration pbiApp)
        {
            using (var client = new HttpClient())
            {
                var form = new Dictionary<string, string>();
                form["grant_type"] = "password";
                form["resource"] = pbiApp.ResourceUrl;
                form["username"] = pbiApp.UserName;
                form["password"] = pbiApp.Password;
                form["client_id"] = pbiApp.ApplicationId.ToString();
                form["client_secret"] = pbiApp.ApplicationSecret;
                form["scope"] = "openid";

                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                using (var formContent = new FormUrlEncodedContent(form))
                using (var response = await client.PostAsync(pbiApp.AuthorityUrl, formContent))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var jsonBody = JObject.Parse(body);

                    var errorToken = jsonBody.SelectToken("error");
                    if (errorToken != null)
                    {
                        throw new Exception(errorToken.Value<string>());
                    }

                    return jsonBody.SelectToken("access_token").Value<string>();
                }
            }
        }

    }
}
