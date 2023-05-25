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


        public IActionResult Calculate(string tupeGuid)
        {
            CalculatorInformation calculatorInformation = new CalculatorInformation();
            switch (tupeGuid)
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
                default:
                    throw new Exception("Неверный GUID типа калькулятора в строке запроса");
            }

            calculatorInformation.ItemsGroup = Guid.Parse(tupeGuid);
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs)
                                                    .FirstOrDefault(c => c.Login == userLogin);
            calculatorInformation.User = User.Identity.Name;
            calculatorInformation.Date = DateTime.Now;
            calculatorInformation.TT = user.TTs.FirstOrDefault();
            calculatorInformation.Reaction = CalculatorDb.CalculatorReaction.FirstOrDefault(c => c.ItemsGroup == calculatorInformation.ItemsGroup &&
                                                                                                  c.FirstHour <= calculatorInformation.Date.Hour &&
                                                                                                  c.LastHour >= calculatorInformation.Date.Hour).Reaction;
            List<TimeDayGroups> timeDayGroups = CalculatorDb.TimeDayGroups.Include(c => c.DayGroup)
                                                                          .Include(c => c.TimeGroup)
                                                                          .Where(c => c.TimeGroup.ItemsGroup == calculatorInformation.ItemsGroup)
                                                                          .OrderBy(c => c.DayGroup.FirstDay)
                                                                          .ThenBy(c => c.TimeGroup.FirstHour)
                                                                          .ToList();


            for (int i = 0; i < timeDayGroups.Count; i++)
            {
                if (timeDayGroups[i].DayGroup.FirstDay <= (int)calculatorInformation.Date.DayOfWeek && timeDayGroups[i].DayGroup.LastDay >= (int)calculatorInformation.Date.DayOfWeek && timeDayGroups[i].TimeGroup.FirstHour <= calculatorInformation.Date.Hour && timeDayGroups[i].TimeGroup.LastHour >= calculatorInformation.Date.Hour)
                {
                    calculatorInformation.ThisTimeDayGroup = timeDayGroups[i];
                    if (i + 1 == timeDayGroups.Count)
                    {
                        calculatorInformation.NextTimeDayGroup = timeDayGroups[0];
                    }
                    else
                    {
                        calculatorInformation.NextTimeDayGroup = timeDayGroups[i + 1];
                    }
                }
            }
            calculatorInformation.ThisPeriodCoefficient = CalculatorDb.ItemsGroupTimeTT_Coefficient.FirstOrDefault(c => c.TimeGroup == calculatorInformation.ThisTimeDayGroup.TimeGroup &&
                                                                                                                        c.TTCODE == calculatorInformation.TT.Restaurant_Sifr).Coefficient;
            calculatorInformation.NextPeriodCoefficient = CalculatorDb.ItemsGroupTimeTT_Coefficient.FirstOrDefault(c => c.TimeGroup == calculatorInformation.NextTimeDayGroup.TimeGroup &&
                                                                                                                        c.TTCODE == calculatorInformation.TT.Restaurant_Sifr).Coefficient;
            calculatorInformation.ThisPeriodCoefficient = Math.Round(calculatorInformation.ThisPeriodCoefficient, 2);
            calculatorInformation.NextPeriodCoefficient = Math.Round(calculatorInformation.NextPeriodCoefficient, 2);



            List<Items> items = CalculatorDb.Items.Where(c => c.ItemsGroup == calculatorInformation.ItemsGroup)
                                                  .OrderBy(c => c.Sequence)  // сортировка для таблицы
                                                  .ToList();
            calculatorInformation.Items = new List<CalculatorItem>();

            foreach (var item in items)
            {
                CalculatorItem thisItem = new CalculatorItem();
                thisItem.ItemOnTT = CalculatorDb.ItemOnTT.FirstOrDefault(c => c.Item == item && c.TTCode == calculatorInformation.TT.Restaurant_Sifr);
                double thisPeriodRestSalesSum = CalculatorDb.AverageSalesPerHour.Where(c => c.TimeDayGroups == calculatorInformation.ThisTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTT == thisItem.ItemOnTT.Guid &&
                                                                                                        c.Hour > calculatorInformation.Date.Hour)
                                                                                             .ToList()
                                                                                             .Select(c => c.Quantity)
                                                                                             .DefaultIfEmpty(0)
                                                                                             .Sum();
                thisItem.AverageRestOfThisPeriod = thisPeriodRestSalesSum
                                                   / (4 * (calculatorInformation.ThisTimeDayGroup.DayGroup.LastDay - calculatorInformation.ThisTimeDayGroup.DayGroup.FirstDay + 1));

                double nextPeriodSalesSum = CalculatorDb.AverageSalesPerHour.Where(c => c.TimeDayGroups == calculatorInformation.NextTimeDayGroup.Guid &&
                                                                                                        c.ItemOnTT == thisItem.ItemOnTT.Guid)
                                                                                             .ToList()
                                                                                             .Select(c => c.Quantity)
                                                                                             .DefaultIfEmpty(0)
                                                                                             .Sum();
                thisItem.AverageNextPer = nextPeriodSalesSum
                                                   / (4 * (calculatorInformation.NextTimeDayGroup.DayGroup.LastDay - calculatorInformation.NextTimeDayGroup.DayGroup.FirstDay + 1));
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
                dbSql.CalculatorLogs.Add(calculatorLog);
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
