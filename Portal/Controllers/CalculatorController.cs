using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models;
using Portal.Models.Calculator;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Calculator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [Authorize(Roles = "calculators")]
    public class CalculatorController : Controller
    {

        private DB.CalculatorDBContext CalculatorDb;
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        private IHttpClientFactory _httpClientFactory;

        public CalculatorController(DB.CalculatorDBContext calculatorDbContext, DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IHttpClientFactory _httpClientFactoryConnect)
        {
            CalculatorDb = calculatorDbContext;
            db = context;
            dbSql = dbSqlContext;
            _httpClientFactory = _httpClientFactoryConnect;
        }


        public IActionResult Vipechka()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора выпечки";
            log.Description = "/Calculator/Vipechka";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public IActionResult Conditerka()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора кондитерки";
            log.Description = "/Calculator/Conditerka";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public IActionResult Sandwitches()
        {
            // логируем

            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора сэндвичей";
            log.Description = "/Calculator/Sandwitches";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public IActionResult Calculate(string typeGuid, string tt)
        {
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора выпечки";
            log.Description = "/Calculator/Vipechka";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save(userAgent, HttpContext.Session.Id);
            CalculatorInformation calculatorInformation = new CalculatorInformation();
            switch (typeGuid.ToUpper())
            {
                case "573B9B93-41A1-4ACA-B740-A98CEF77E935":
                    calculatorInformation.Name = "Калькулятор Выпечки";
                    calculatorInformation.PicturePath = "~/svg/color_panels/bread.svg";
                    break;
                case "9C42DDD0-3ABE-4105-AFA1-BFDA989C3836":
                    calculatorInformation.Name = "Калькулятор Сэндвичей";
                    calculatorInformation.PicturePath = "~/svg/color_panels/sandwich.svg";
                    break;
                case "9FA5B2FC-91F1-4771-BF06-F7C3E7E37359":
                    calculatorInformation.Name = "Калькулятор Дефроста";
                    calculatorInformation.PicturePath = "~/svg/color_panels/cake.svg";
                    break;
                case "43E2F47F-8729-49C6-8507-64DFEDBC6BBC":
                    calculatorInformation.Name = "Калькулятор Хлеба";
                    calculatorInformation.PicturePath = "~/svg/color_panels/breads.svg";
                    break;
                case "06BE0412-8D99-4111-99A9-98172E0D3930":
                    calculatorInformation.Name = "Прочий ассортимент";
                    calculatorInformation.PicturePath = "~/svg/color_panels/other-food.png";
                    break;
                default:
                    throw new Exception("Неверный GUID типа калькулятора в строке запроса");
            }

            calculatorInformation.ItemsGroup = CalculatorDb.ItemsGroups.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;

            List<Models.MSSQL.Location.Location> location = dbSql.Locations.Include(x => x.LocationType)
                                                                           .ToList();

            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs.Where(d => d.CloseDate == null)).FirstOrDefault(c => c.Login == userLogin);

            for (int i = 0; i < user.TTs.Count; i++)
            {
                var ttWithType = location.FirstOrDefault(x => x.RKCode == user.TTs[i]?.Restaurant_Sifr);
                if (ttWithType?.LocationType?.Name == "УЦ" || ttWithType?.LocationType?.Name == "Офис")
                {
                    user.TTs.RemoveAll(x => x.Id == user.TTs[i].Id);
                }
            }

            calculatorInformation.User = User.Identity.Name;
            calculatorInformation.Date = DateTime.Now;
            if (String.IsNullOrEmpty(tt))
            {
                calculatorInformation.TTs = user.TTs.ToList();
            }
            else
            {
                calculatorInformation.TTs = user.TTs.Where(c => c.Restaurant_Sifr == int.Parse(tt)).ToList();
            }
            if (calculatorInformation.TTs.Count == 0)
            {
                throw new Exception($"В БД отсутствует ТТ с кодом {tt}");
            }
            if (calculatorInformation.TTs.Count != 1)
            {
                return PartialView(calculatorInformation);
            }
            calculatorInformation.Reaction = CalculatorDb.CalculatorReaction.FirstOrDefault(c => c.ItemsGroup == calculatorInformation.ItemsGroup.Guid &&
                                                                                                  c.FirstHour <= calculatorInformation.Date.Hour &&
                                                                                                  c.LastHour >= calculatorInformation.Date.Hour).Reaction;
            List<TimeGroups> timeGroups = CalculatorDb.TimeGroups.Where(c => c.ItemsGroup == calculatorInformation.ItemsGroup.Guid)
                                                                 .OrderBy(c => c.FirstHour)
                                                                 .ToList();
            List<DayGroups> dayGroups = CalculatorDb.DayGroups.OrderBy(c => c.FirstDay)
                                                              .ToList();
            TimeGroups thisTimeGroup = new TimeGroups();
            TimeGroups nextTimeGroup = new TimeGroups();
            TimeGroups nextSecondTimeGroup = new TimeGroups();

            DayGroups thisDayGroup = new DayGroups();
            DayGroups nextDayGroup = new DayGroups();

            DayGroups spesialToday = CalculatorDb.SpecialDays.Include(c => c.DayGroups).FirstOrDefault(c => c.Date == DateTime.Today)?.DayGroups;
            DayGroups spesialTomorrow = CalculatorDb.SpecialDays.Include(c => c.DayGroups).FirstOrDefault(c => c.Date == DateTime.Today.AddDays(1))?.DayGroups;

            for (int i = 0; i < dayGroups.Count; i++)
            {
                if (dayGroups[i].FirstDayUS <= (int)calculatorInformation.Date.DayOfWeek && dayGroups[i].LastDayUS >= (int)calculatorInformation.Date.DayOfWeek)
                {
                    thisDayGroup = dayGroups[i];
                    if (thisDayGroup.LastDayUS != (int)calculatorInformation.Date.DayOfWeek)
                    {
                        nextDayGroup = dayGroups[i];
                        break;
                    }
                    if (dayGroups.Count == i + 1)
                    {
                        nextDayGroup = dayGroups[0];
                        break;
                    }
                    nextDayGroup = dayGroups[i + 1];
                    break;
                }
            }

            if (spesialToday != null)
            {
                thisDayGroup = spesialToday;
            }
            if (spesialTomorrow != null)
            {
                nextDayGroup = spesialTomorrow;
            }

            for (int i = 0; i < timeGroups.Count; i++)
            {
                if (timeGroups[i].FirstHour <= calculatorInformation.Date.Hour && timeGroups[i].LastHour >= calculatorInformation.Date.Hour)
                {
                    thisTimeGroup = timeGroups[i];
                    calculatorInformation.ThisTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == thisTimeGroup && c.DayGroup == thisDayGroup);


                    if (timeGroups.Count == 1)
                    {
                        nextTimeGroup = timeGroups[0];
                        nextSecondTimeGroup = timeGroups[0];
                        calculatorInformation.NextTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextTimeGroup && c.DayGroup == nextDayGroup);
                        calculatorInformation.NextSecondTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextSecondTimeGroup && c.DayGroup == nextDayGroup);
                        break;
                    }



                    if (timeGroups.Count == i + 1)
                    {
                        nextTimeGroup = timeGroups[0];
                        nextSecondTimeGroup = timeGroups[1];
                        calculatorInformation.NextTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextTimeGroup && c.DayGroup == nextDayGroup);
                        calculatorInformation.NextSecondTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextSecondTimeGroup && c.DayGroup == nextDayGroup);
                        break;
                    }

                    if (timeGroups.Count == i + 2)
                    {
                        nextTimeGroup = timeGroups[i + 1];
                        nextSecondTimeGroup = timeGroups[0];
                        calculatorInformation.NextTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextTimeGroup && c.DayGroup == thisDayGroup);
                        calculatorInformation.NextSecondTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextSecondTimeGroup && c.DayGroup == nextDayGroup);
                        break;
                    }
                    nextTimeGroup = timeGroups[i + 1];
                    nextSecondTimeGroup = timeGroups[i + 2];
                    calculatorInformation.NextTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextTimeGroup && c.DayGroup == thisDayGroup);
                    calculatorInformation.NextSecondTimeDayGroup = CalculatorDb.TimeDayGroups.FirstOrDefault(c => c.TimeGroup == nextSecondTimeGroup && c.DayGroup == thisDayGroup);
                    break;
                }
            }

            if (calculatorInformation.ThisTimeDayGroup == null)
            {
                throw new Exception("Период отсутствует в БД");
            }
            calculatorInformation.ThisPeriodCoefficient = CalculatorDb.ItemsGroupTimeTT_Coefficient.FirstOrDefault(c => c.TimeGroup == calculatorInformation.ThisTimeDayGroup.TimeGroup &&
                                                                                                                        c.TTCODE == calculatorInformation.TTs[0].Restaurant_Sifr).Coefficient;
            calculatorInformation.NextPeriodCoefficient = CalculatorDb.ItemsGroupTimeTT_Coefficient.FirstOrDefault(c => c.TimeGroup == calculatorInformation.NextTimeDayGroup.TimeGroup &&
                                                                                                                        c.TTCODE == calculatorInformation.TTs[0].Restaurant_Sifr).Coefficient;
            calculatorInformation.NextSecondPeriodCoefficient = CalculatorDb.ItemsGroupTimeTT_Coefficient.FirstOrDefault(c => c.TimeGroup == calculatorInformation.NextSecondTimeDayGroup.TimeGroup &&
                                                                                                                        c.TTCODE == calculatorInformation.TTs[0].Restaurant_Sifr).Coefficient;
            calculatorInformation.ThisPeriodCoefficient = Math.Round(calculatorInformation.ThisPeriodCoefficient, 2);
            calculatorInformation.NextPeriodCoefficient = Math.Round(calculatorInformation.NextPeriodCoefficient, 2);
            calculatorInformation.NextSecondPeriodCoefficient = Math.Round(calculatorInformation.NextSecondPeriodCoefficient, 2);



            List<Models.MSSQL.Calculator.Items> items = CalculatorDb.Items.Where(c => c.ItemsGroup == calculatorInformation.ItemsGroup.Guid)
                                                  .OrderBy(c => c.Sequence)  // сортировка для таблицы
                                                  .ToList();

            calculatorInformation.Items = new List<CalculatorItem>();

            List<Portal.Models.MSSQL.CalculatorLogsTest> calculatorLogTests = new();

            foreach (var item in items)
            {
                CalculatorItem thisItem = new CalculatorItem();
                thisItem.SettlementDaysRestOfThisPeriod = 1;
                thisItem.SettlementDaysNextPer = 1;
                thisItem.SettlementDaysSecondNextPer = 1;
                thisItem.ItemOnTT = CalculatorDb.ItemOnTT.FirstOrDefault(c => c.Item == item && c.TTCode == calculatorInformation.TTs[0].Restaurant_Sifr);

                CalculatorLogsTest calulatorLogs = dbSql.CalculatorLogsTest.Where(x => x.TTCode == calculatorInformation.TTs[0].Restaurant_Sifr && 
                                                                                       x.ItemName == item.Name &&
                                                                                       x.Date > DateTime.Now.AddMinutes(-30))
                                                                           .OrderByDescending(x => x.Date)
                                                                           .FirstOrDefault();

                calculatorLogTests.Add(calulatorLogs);
                

                List<NumberOfSales> thisPeriodRestSales = CalculatorDb.NumberOfSales.Where(c => c.TimeDayGroupsGUID == calculatorInformation.ThisTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTTGUID == thisItem.ItemOnTT.Guid &&
                                                                                                        c.Hour > calculatorInformation.Date.Hour)
                                                                                                .ToList();
                thisItem.SumRestOfThisPeriod = thisPeriodRestSales.Select(c => c.Quantity)
                                                                   .DefaultIfEmpty(0)
                                                                   .Sum();
                thisItem.SumProductionPeriod = thisPeriodRestSales.Where(c => c.Hour < calculatorInformation.Date.Hour + calculatorInformation.ItemsGroup.HourForProduction)
                                                                .Select(c => c.Quantity)
                                                                .DefaultIfEmpty(0)
                                                                .Sum();

                if (thisPeriodRestSales.Count != 0)
                {
                    thisItem.SettlementDaysRestOfThisPeriod = thisPeriodRestSales[0].SettlementDays;
                }

                thisItem.AverageRestOfThisPeriod = thisItem.SumRestOfThisPeriod / thisItem.SettlementDaysRestOfThisPeriod;

                thisItem.AverageProductionPeriod = thisItem.SumProductionPeriod / thisItem.SettlementDaysRestOfThisPeriod; ;

                thisItem.SumNextPer = CalculatorDb.NumberOfSales.Where(c => c.TimeDayGroupsGUID == calculatorInformation.NextTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTTGUID == thisItem.ItemOnTT.Guid)
                                                                                             .ToList()
                                                                                             .Select(c => c.Quantity)
                                                                                             .DefaultIfEmpty(0)
                                                                                             .Sum();
                var settlementDaysNextPer = CalculatorDb.NumberOfSales.FirstOrDefault(c => c.TimeDayGroupsGUID == calculatorInformation.NextTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTTGUID == thisItem.ItemOnTT.Guid);
                if (settlementDaysNextPer != null)
                {
                    thisItem.SettlementDaysNextPer = settlementDaysNextPer.SettlementDays;
                }

                thisItem.AverageNextPer = thisItem.SumNextPer / thisItem.SettlementDaysNextPer;

                thisItem.SumSecondNextPer = CalculatorDb.NumberOfSales.Where(c => c.TimeDayGroupsGUID == calculatorInformation.NextSecondTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTTGUID == thisItem.ItemOnTT.Guid)
                                                                                             .ToList()
                                                                                             .Select(c => c.Quantity)
                                                                                             .DefaultIfEmpty(0)
                                                                                             .Sum();

                var settlementDaysSecondNextPer = CalculatorDb.NumberOfSales.FirstOrDefault(c => c.TimeDayGroupsGUID == calculatorInformation.NextSecondTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTTGUID == thisItem.ItemOnTT.Guid);
                if (settlementDaysSecondNextPer != null)
                {
                    thisItem.SettlementDaysSecondNextPer = settlementDaysSecondNextPer.SettlementDays;
                }

                thisItem.AverageSecondNextPer = thisItem.SumSecondNextPer / thisItem.SettlementDaysSecondNextPer;

                calculatorInformation.Items.Add(thisItem);
            }
            ViewBag.logs = calculatorLogTests;
            return PartialView(calculatorInformation);
        }

        public IActionResult TabMenu()
        {
            return PartialView();
        }

        public IActionResult CalculateCoefficient()
        {
            CalculateCoefficientModel calculateCoefficientModel = new CalculateCoefficientModel();
            calculateCoefficientModel.Items = CalculatorDb.Items.ToList();
            calculateCoefficientModel.TT = CalculatorDb.TT.ToList();
            calculateCoefficientModel.TimeGroups = CalculatorDb.TimeGroups.ToList();
            return PartialView(calculateCoefficientModel);
        }

        public IActionResult CalculateCoefficientTable(string k, string TT, string item, string timeGroup)
        {
            CalculateCoefficientTableModel calculateCoefficientTableModel = new CalculateCoefficientTableModel();
            int TTRkCode = int.Parse(TT);
            int SKURkCode = int.Parse(item);
            Guid timeGroupGuid = Guid.Empty;
            Guid.TryParse(timeGroup, out timeGroupGuid);
            switch (k)
            {
                case "1":
                    calculateCoefficientTableModel.k = 1;
                    List<ItemOnTT> itemOnTTs = new List<ItemOnTT>();
                    if (SKURkCode == 0 && TTRkCode == 0)
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.ToList();
                    }
                    else if (SKURkCode == 0)
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.Where(c => c.TTCode == TTRkCode).ToList();
                    }
                    else if (TTRkCode == 0)
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.Include(c => c.Item)
                                                                        .Where(c => c.Item.RkCode == SKURkCode).ToList();
                    }
                    else
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.Include(c => c.Item)
                                                                       .Where(c => c.Item.RkCode == SKURkCode && c.TTCode == TTRkCode).ToList();
                    }
                    calculateCoefficientTableModel.ItemOnTTs = itemOnTTs;
                    break;
                case "2":
                    calculateCoefficientTableModel.k = 2;
                    List<Models.MSSQL.Calculator.Items> items = new List<Models.MSSQL.Calculator.Items>();
                    if (SKURkCode == 0)
                    {
                        items = CalculatorDb.Items.ToList();
                    }
                    else
                    {
                        items = CalculatorDb.Items.Where(c => c.RkCode == SKURkCode).ToList();
                    }
                    calculateCoefficientTableModel.Items = items;
                    break;
                case "3":
                    calculateCoefficientTableModel.k = 3;
                    List<ItemsGroupTimeTT_Coefficient> itemsGroupTimeTT_Coefficients = new List<ItemsGroupTimeTT_Coefficient>();
                    if (TTRkCode == 0 && timeGroup == "0")
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.ToList();
                    }
                    else if (TTRkCode == 0)
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.Include(c => c.TimeGroup)
                                                                                                 .Where(c => c.TimeGroup.Guid == timeGroupGuid).ToList();
                    }
                    else if (timeGroup == "0")
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.Where(c => c.TTCODE == TTRkCode).ToList();
                    }
                    else
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.Include(c => c.TimeGroup)
                                                                                                     .Where(c => c.TTCODE == TTRkCode && c.TimeGroup.Guid == timeGroupGuid).ToList();
                    }
                    calculateCoefficientTableModel.ItemsGroupTimeTT_Coefficients = itemsGroupTimeTT_Coefficients;
                    break;
            }
            return PartialView(calculateCoefficientTableModel);
        }

        [HttpPost]
        public IActionResult ChangeCalculateCoefficient(string k, string delta, string TT, string item, string timeGroup)
        {
            int TTRkCode = int.Parse(TT);
            int SKURkCode = int.Parse(item);
            Guid timeGroupGuid = Guid.Empty;
            Guid.TryParse(timeGroup, out timeGroupGuid);
            delta = delta.Replace('.', ',');
            double Delta = 0;
            if (!double.TryParse(delta, out Delta))
            {
                return BadRequest("invalid delta");
            }
            switch (k)
            {
                case "1":
                    List<ItemOnTT> itemOnTTs = new List<ItemOnTT>();
                    if (SKURkCode == 0 && TTRkCode == 0)
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.ToList();
                    }
                    else if (SKURkCode == 0)
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.Where(c => c.TTCode == TTRkCode).ToList();
                    }
                    else if (TTRkCode == 0)
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.Include(c => c.Item)
                                                                        .Where(c => c.Item.RkCode == SKURkCode).ToList();
                    }
                    else
                    {
                        itemOnTTs = CalculatorDb.ItemOnTT.Include(c => c.Item)
                                                                       .Where(c => c.Item.RkCode == SKURkCode && c.TTCode == TTRkCode).ToList();
                    }
                    foreach (var itemOnTT in itemOnTTs)
                    {
                        itemOnTT.Coefficient += Delta;
                    }
                    break;
                case "2":
                    List<Models.MSSQL.Calculator.Items> items = new List<Models.MSSQL.Calculator.Items>();
                    if (SKURkCode == 0)
                    {
                        items = CalculatorDb.Items.ToList();
                    }
                    else
                    {
                        items = CalculatorDb.Items.Where(c => c.RkCode == SKURkCode).ToList();
                    }
                    foreach (var item1 in items)
                    {
                        item1.Coefficient += Delta;
                    }
                    break;
                case "3":
                    List<ItemsGroupTimeTT_Coefficient> itemsGroupTimeTT_Coefficients = new List<ItemsGroupTimeTT_Coefficient>();
                    if (TTRkCode == 0 && timeGroup == "0")
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.ToList();
                    }
                    else if (TTRkCode == 0)
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.Include(c => c.TimeGroup)
                                                                                                 .Where(c => c.TimeGroup.Guid == timeGroupGuid).ToList();
                    }
                    else if (timeGroup == "0")
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.Where(c => c.TTCODE == TTRkCode).ToList();
                    }
                    else
                    {
                        itemsGroupTimeTT_Coefficients = CalculatorDb.ItemsGroupTimeTT_Coefficient.Include(c => c.TimeGroup)
                                                                                                     .Where(c => c.TTCODE == TTRkCode && c.TimeGroup.Guid == timeGroupGuid).ToList();
                    }
                    foreach (var itemsGroupTimeTT_Coefficient in itemsGroupTimeTT_Coefficients)
                    {
                        itemsGroupTimeTT_Coefficient.Coefficient += Delta;
                    }
                    break;
            }
            CalculatorDb.SaveChanges();
            return Ok();
        }


        public IActionResult CalculateSKU()
        {
            return PartialView();
        }


        public IActionResult CalculateSKUTable()
        {
            List<Items> items = CalculatorDb.Items.ToList();
            List<ItemsGroups> itemGroups = CalculatorDb.ItemsGroups.ToList();
            List<SKUTableItem> sKUTableItems = new List<SKUTableItem>();
            foreach (var item in items)
            {
                sKUTableItems.Add(new SKUTableItem() { Item = item, GroupName = itemGroups.FirstOrDefault(c => c.Guid == item.ItemsGroup).Name });
            }
            return PartialView(sKUTableItems);
        }

        public IActionResult CalculateSKUEdit(string SKUId)
        {
            int id = Int32.Parse(SKUId);
            SKUEditItem sKUEditItem = new SKUEditItem();
            sKUEditItem.ItemsGroups = CalculatorDb.ItemsGroups.ToList();
            if (id == 0)
            {
                return PartialView(sKUEditItem);
            }
            sKUEditItem.Item = CalculatorDb.Items.FirstOrDefault(c => c.RkCode == id);
            sKUEditItem.ItemsGroup = CalculatorDb.ItemsGroups.FirstOrDefault(c => c.Guid == sKUEditItem.Item.ItemsGroup);
            return PartialView(sKUEditItem);
        }

        public IActionResult CalculateSpesialDays(string SKUId)
        {
            CalculateSpecialDays calculateSpecialDays = new CalculateSpecialDays();

            calculateSpecialDays.SpecialDays = CalculatorDb.SpecialDays.Include(c => c.DayGroups).ToList();
            calculateSpecialDays.DayGroups = CalculatorDb.DayGroups.ToList();
            return PartialView(calculateSpecialDays);
        }


        [HttpPost]

        public IActionResult SaveSKU([FromBody] Items SKU)
        {
            if (SKU == null)
            {
                return BadRequest("SKU is null");
            }
            Items item = CalculatorDb.Items.FirstOrDefault(c => c.RkCode == SKU.RkCode);
            if (item is null) // создание СКЮ
            {
                CalculatorDb.Items.Add(SKU);
            }
            else // изменение СКЮ
            {
                item.Name = SKU.Name;
                item.RkCode = SKU.RkCode;
                item.ItemsGroup = SKU.ItemsGroup;
                item.Coefficient = SKU.Coefficient;
                item.Sequence = SKU.Sequence;
                item.DefrostTime = SKU.DefrostTime;
                item.BakingMode = SKU.BakingMode;
                item.MinShowCase = SKU.MinShowCase;
            }
            CalculatorDb.SaveChanges();
            return Ok();
        }

        [HttpDelete]
        public IActionResult DeleteSKU(string RkCode)
        {
            int skuCode = int.Parse(RkCode);
            Items item = CalculatorDb.Items.FirstOrDefault(c => c.RkCode == skuCode);
            if (item is null) // создание СКЮ
            {
                return BadRequest("СКЮ " + skuCode + " отсутствует в БД");
            }
            CalculatorDb.Items.Remove(item);
            CalculatorDb.SaveChanges();
            return Ok();
        }

        [HttpPost]
        public IActionResult SaveSpecialDay([FromBody] FrontSpecialDay frontSpecialDay)
        {
            if (frontSpecialDay == null)
            {
                return BadRequest("data is null");
            }
            if (frontSpecialDay.Id == null)
            {
                SpecialDay specialDay = new SpecialDay();
                specialDay.DayGroups = CalculatorDb.DayGroups.FirstOrDefault(c => c.Guid == frontSpecialDay.DayGroupsGuid);
                specialDay.Date = frontSpecialDay.Date;
                CalculatorDb.SpecialDays.Add(specialDay);
            }
            else
            {
                SpecialDay specialDay = CalculatorDb.SpecialDays.FirstOrDefault(c => c.Id == frontSpecialDay.Id);
                specialDay.Date = frontSpecialDay.Date;
                specialDay.DayGroups = CalculatorDb.DayGroups.FirstOrDefault(c => c.Guid == frontSpecialDay.DayGroupsGuid);
            }
            CalculatorDb.SaveChanges(true);
            return Ok();
        }

        [HttpDelete]
        public IActionResult DeleteSpesialDay(string Id)
        {
            int id = int.Parse(Id);
            SpecialDay specialDay = CalculatorDb.SpecialDays.FirstOrDefault(c => c.Id == id);
            if (specialDay == null)
            {
                return BadRequest("invalid id");
            }
            CalculatorDb.SpecialDays.Remove(specialDay);
            CalculatorDb.SaveChanges();
            return Ok();
        }

        public IActionResult CalculateChart()
        {
            return PartialView();
        }



        public async Task<IActionResult> LogSave(string logjsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                //if (!Global.Functions.CheckSessionID(dbSql, HttpContext.Session.Id))
                //{
                //    throw new Exception("401");
                //}

                var idFromSql = db.Users.FirstOrDefault(x => x.Name == User.Identity.Name).Id;
                var oldSession = dbSql.UserSessions.FirstOrDefault(x => x.Id == idFromSql);

                if (oldSession.Date.AddHours(1) < DateTime.Now)
                {
                    throw new Exception("401");
                }

                logjsn = logjsn.Replace("%bkspc%", " ");
                CalculatorLog calculatorLog = JsonConvert.DeserializeObject<CalculatorLog>(logjsn);
                calculatorLog.Date = DateTime.Now;

                if (HttpContext.Session.IsAvailable)
                {
                    calculatorLog.SessionId = Guid.Parse(oldSession.SessionID);
                }
                else
                {
                    throw new InvalidOperationException("Session is not available");
                }

                var httpClient = _httpClientFactory.CreateClient();
                string microserviceUrl = "http://rknet-server:45732/Buffer";
                HttpResponseMessage response = await httpClient.PostAsJsonAsync(microserviceUrl, calculatorLog);

                if (response.IsSuccessStatusCode)
                {
                    result.Ok = true;
                }
            }
            catch (Exception ex)
            {
                logjsn = logjsn.Replace("%bkspc%", " ");
                WriteErrorToLogFile(logjsn, ex.Message);
                result.ErrorMessage = ex.Message;
                result.Ok = false;
                return new ObjectResult(result);
            }
            return new ObjectResult(result);
        }

        static void WriteErrorToLogFile(string logjsn, string error)
        {
            string errorLogFilePath = "errorLog.txt";

            // Используем явное указание пространства имен для File из System.IO
            using (StreamWriter writer = System.IO.File.AppendText(errorLogFilePath))
            {
                writer.WriteLine($"Время ошибки: {DateTime.Now}");
                writer.WriteLine($"Данные из JSON: {logjsn}");
                writer.WriteLine(error);
                writer.WriteLine(new string('-', 50));
            }

            Console.WriteLine("Информация об ошибке успешно записана в файл.");
        }

    }

    public class CalculateCoefficientModel
    {
        public List<Models.MSSQL.Calculator.Items> Items { get; set; }
        public List<Models.MSSQL.Calculator.TT> TT { get; set; }
        public List<Models.MSSQL.Calculator.TimeGroups> TimeGroups { get; set; }
    }

    public class CalculateCoefficientTableModel
    {

        public int k { get; set; }
        public List<Models.MSSQL.Calculator.Items> Items { get; set; }
        public List<Models.MSSQL.Calculator.ItemOnTT> ItemOnTTs { get; set; }
        public List<Models.MSSQL.Calculator.ItemsGroupTimeTT_Coefficient> ItemsGroupTimeTT_Coefficients { get; set; }
    }

    public class SKUTableItem
    {
        public Items Item { get; set; }
        public string GroupName { get; set; }

    }

    public class SKUEditItem
    {
        public Items Item { get; set; }
        public ItemsGroups ItemsGroup { get; set; }
        public List<ItemsGroups> ItemsGroups { get; set; }

    }

    public class CalculateSpecialDays
    {
        public List<SpecialDay> SpecialDays { get; set; }
        public List<DayGroups> DayGroups { get; set; }
    }

    public class FrontSpecialDay
    {
        public int? Id { get; set; }
        public DateTime Date { get; set; }
        public Guid DayGroupsGuid { get; set; }
    }

}
