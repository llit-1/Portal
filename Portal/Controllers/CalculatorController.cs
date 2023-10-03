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
using System.Linq;
using System.Security.Claims;

namespace Portal.Controllers
{
    [Authorize(Roles = "calculators")]
    public class CalculatorController : Controller
    {

        private DB.CalculatorDBContext CalculatorDb;
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        public CalculatorController(DB.CalculatorDBContext calculatorDbContext, DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            CalculatorDb = calculatorDbContext;
            db = context;
            dbSql = dbSqlContext;
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
                default:
                    throw new Exception("Неверный GUID типа калькулятора в строке запроса");
            }

            calculatorInformation.ItemsGroup = CalculatorDb.ItemsGroups.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs.Where(d => d.CloseDate == null && d.Type != null && d.Type.Id != 3))
                                                    .FirstOrDefault(c => c.Login == userLogin);
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



            List<Items> items = CalculatorDb.Items.Where(c => c.ItemsGroup == calculatorInformation.ItemsGroup.Guid)
                                                  .OrderBy(c => c.Sequence)  // сортировка для таблицы
                                                  .ToList();
            calculatorInformation.Items = new List<CalculatorItem>();

            foreach (var item in items)
            {
                CalculatorItem thisItem = new CalculatorItem();
                thisItem.SettlementDaysRestOfThisPeriod = 1;
                thisItem.SettlementDaysNextPer = 1;
                thisItem.SettlementDaysSecondNextPer = 1;
                thisItem.ItemOnTT = CalculatorDb.ItemOnTT.FirstOrDefault(c => c.Item == item && c.TTCode == calculatorInformation.TTs[0].Restaurant_Sifr);



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
            return PartialView(calculatorInformation);
        }

        public IActionResult LogSave(string logjsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                logjsn = logjsn.Replace("%bkspc%", " ");
                CalculatorLog calculatorLog = JsonConvert.DeserializeObject<CalculatorLog>(logjsn);
                calculatorLog.Date = DateTime.Now;
                calculatorLog.SessionId = Guid.Parse(HttpContext.Session.Id);
                dbSql.CalculatorLogs.Add(calculatorLog);
                CalculatorLogTest calculatorLogsTest = new CalculatorLogTest(calculatorLog);
                dbSql.CalculatorLogsTest.Add(calculatorLogsTest);
                dbSql.SaveChanges();
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
            return new ObjectResult(result);
        }
    }

}
