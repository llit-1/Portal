using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Personality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [Authorize]
    public class SalaryController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        private readonly IHttpClientFactory httpClientFactory;

        public SalaryController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IHttpClientFactory httpClientFactoryConnect)
        {
            db = context;
            dbSql = dbSqlContext;
            httpClientFactory = httpClientFactoryConnect;
        }

        public IActionResult Salary()
        {
            List<Models.MSSQL.Location.Location> locations = dbSql.Locations.OrderBy(loc => loc.Name).ToList();

            return PartialView(locations);
        }

        public async Task<IActionResult> GetTimeSheets(Guid? locations = null, Guid? Person = null, DateTime? Start = null, DateTime? End = null, int? Month = null, int? Year = null)
        {
            if(Start == null && End == null && Month == null)
            {
                End = DateTime.Now;
                Start = DateTime.Now.AddDays(-155);
            }


            DateTime? rangeStart = Start?.Date;
            DateTime? rangeEndExclusive = End?.Date.AddDays(1);

            if (Month.HasValue)
            {
                int year = Year ?? Start?.Year ?? End?.Year ?? DateTime.Now.Year;
                DateTime monthStart = new DateTime(year, Month.Value, 1);

                // Режим "месяц": с 1-го числа до конца выбранного месяца.
                rangeStart = monthStart;
                rangeEndExclusive = monthStart.AddMonths(1);
            }

            IQueryable<Models.MSSQL.TimeSheet> query = dbSql.TimeSheets
                .AsNoTracking()
                .Where(x => x.Personalities != null && x.Location != null && x.JobTitle != null);

            if (Person.HasValue)
            {
                query = query.Where(x => x.Personalities.Guid == Person.Value);
            }

            if (locations.HasValue)
            {
                query = query.Where(x => x.Location.Guid == locations.Value);
            }

            if (rangeStart.HasValue)
            {
                query = query.Where(x => x.Begin >= rangeStart.Value);
            }

            if (rangeEndExclusive.HasValue)
            {
                query = query.Where(x => x.End < rangeEndExclusive.Value);
            }

            var timeSheets = await query
                .OrderByDescending(x => x.Begin)
                .Select(x => new
                {
                    guid = x.Guid,
                    personalityGuid = x.Personalities != null ? (Guid?)x.Personalities.Guid : null,
                    fio = dbSql.PersonalityVersions
                        .Where(p => p.Actual == 1 && p.Personalities != null && p.Personalities.Guid == x.Personalities.Guid)
                        .Select(p => (p.Surname + " " + p.Name + " " + (p.Patronymic ?? string.Empty)).Trim())
                        .FirstOrDefault() ?? x.Personalities.Name,
                    location = x.Location != null ? x.Location.Name : string.Empty,
                    position = x.JobTitle != null ? x.JobTitle.Name : string.Empty,
                    locationGuid = x.Location != null ? (Guid?)x.Location.Guid : null,
                    begin = x.Begin,
                    end = x.End,
                    absence = x.Absence,
                    baseRate = x.BaseRate,
                    locationCashBonus = x.LocationCashBonus,
                    experienceCashBonus = x.ExperienceCashBonus,
                    personalCashBonus = x.PersonalCashBonus,
                    totalSalary = x.TotalSalary
                })
                .ToListAsync();

            return Json(timeSheets);
        }

        public async Task<IActionResult> GetPersonalityList(string fio)
        {
            fio = (fio ?? string.Empty).Trim();

            if (fio.Length < 2)
            {
                return Json(new List<object>());
            }

            var personalities = await dbSql.PersonalityVersions
                .AsNoTracking()
                .Where(p => p.Actual == 1 && (p.Surname + " " + p.Name + " " + (p.Patronymic ?? string.Empty)).Contains(fio))
                .OrderBy(p => p.Surname)
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Patronymic)
                .Take(30)
                .Select(p => new
                {
                    personalityVersionGuid = p.Guid,
                    personalityGuid = p.Personalities != null ? (Guid?)p.Personalities.Guid : null,
                    fio = (p.Surname + " " + p.Name + " " + (p.Patronymic ?? string.Empty)).Trim()
                })
                .ToListAsync();

            return Json(personalities);
        }

        public async Task<IActionResult> GetSalaryDataForTimeSheet(Guid guid)
        {
            string requestUrl = $"http://rknet-server:1571/api/CalculateSalary/settimesheetsalary?guid={Uri.EscapeDataString(guid.ToString())}";

            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = "Не удалось пересчитать зарплату для табеля"
                    });
                }
            }
            catch
            {
                return StatusCode(502, new
                {
                    message = "Ошибка обращения к сервису расчета зарплаты"
                });
            }

            var timeSheet = await dbSql.TimeSheets
                .AsNoTracking()
                .Where(x => x.Guid == guid)
                .Select(x => new
                {
                    guid = x.Guid,
                    begin = x.Begin,
                    end = x.End,
                    baseRate = x.BaseRate,
                    locationCashBonus = x.LocationCashBonus,
                    experienceCashBonus = x.ExperienceCashBonus,
                    personalCashBonus = x.PersonalCashBonus,
                    totalSalary = x.TotalSalary
                })
                .FirstOrDefaultAsync();

            if (timeSheet == null)
            {
                return NotFound();
            }

            return Json(timeSheet);
        }

        [HttpPost]
        public async Task<IActionResult> CalculateSalaries([FromBody] List<Guid> guids)
        {
            var calculateResult = await Portal.Global.Functions.CalculateSalariesAsync(guids);

            if (!calculateResult.IsSuccess)
            {
                return StatusCode(calculateResult.StatusCode, new
                {
                    message = calculateResult.Message
                });
            }

            return Ok(new
            {
                calculated = calculateResult.Calculated
            });
        }

        public async Task<IActionResult> SalarySettings()
        {
            const string requestUrl = "http://rknet-server:1571/api/Edit/GetBaseTable";
            List<BaseTableItem> baseTableItems = new List<BaseTableItem>();
            SalarySettingsBaseTable salarySettingsBaseTable = new SalarySettingsBaseTable
            {
                Items = new List<BaseTableItem>(),
                jobTitles = new List<JobTitle>()
            };

            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.BaseTableLoadError = $"Не удалось загрузить таблицу ставок (HTTP {(int)response.StatusCode})";
                    salarySettingsBaseTable.jobTitles = dbSql.JobTitles.AsNoTracking().ToList();
                    return PartialView(salarySettingsBaseTable);
                }

                baseTableItems = await response.Content.ReadFromJsonAsync<List<BaseTableItem>>() ?? new List<BaseTableItem>();
                salarySettingsBaseTable.Items = baseTableItems;
            }
            catch
            {
                ViewBag.BaseTableLoadError = "Ошибка обращения к сервису таблицы ставок";
            }

            salarySettingsBaseTable.jobTitles = dbSql.JobTitles.AsNoTracking().ToList();

            return PartialView(salarySettingsBaseTable);
        }

        public class SalarySettingsBaseTable 
        {
            public List<BaseTableItem> Items { get; set; }
            public List<JobTitle> jobTitles { get; set; }
        }

        public class BaseTableItem
        {
            public int RuleId { get; set; }
            public Models.MSSQL.Personality.JobTitle? MainJob { get; set; }
            public Models.MSSQL.Personality.JobTitle? RealJob { get; set; }
            public string BSM { get; set; }
            public string BAK { get; set; }
            public string EXK { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
            public bool PartTimer { get; set; }
        }
    }
}
