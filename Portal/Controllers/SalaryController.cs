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
                jobTitles = new List<JobTitle>(),
                Locations = new List<Portal.Models.MSSQL.Location.Location>()
            };

            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                var factoryTypeGuid = Guid.Parse("94AD659C-AF5B-4CA0-50AD-08DBDF6ABE84");
                var officeTypeGuid = Guid.Parse("B0E427F9-8996-4C03-33C1-08DBDF713401");

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.BaseTableLoadError = $"Не удалось загрузить таблицу ставок (HTTP {(int)response.StatusCode})";
                    return PartialView(salarySettingsBaseTable);
                }

                baseTableItems = await response.Content.ReadFromJsonAsync<List<BaseTableItem>>() ?? new List<BaseTableItem>();
                salarySettingsBaseTable.Items = baseTableItems;
                salarySettingsBaseTable.jobTitles = dbSql.JobTitles.AsNoTracking().ToList();
                salarySettingsBaseTable.Locations = dbSql.Locations.Include(X => X.LocationType)
                    .Where(x => x.LocationType != null &&
                                (x.LocationType.Guid == factoryTypeGuid ||
                                 x.LocationType.Guid == officeTypeGuid))
                    .OrderBy(x => x.Name)
                    .ToList();
            }
            catch
            {
                ViewBag.BaseTableLoadError = "Ошибка обращения к сервису таблицы ставок";
            }

            return PartialView(salarySettingsBaseTable);
        }

        [HttpGet]
        public async Task<IActionResult> SalarySettingsSpecialLocations()
        {
            const string requestUrl = "http://rknet-server:1571/api/Edit/GetLocationTable";
            var items = await LoadSalarySettingsItemsAsync<LocationTableItem>(requestUrl);

            return PartialView("_SalarySettingsSpecialLocations", items);
        }

        [HttpPost]
        public async Task<IActionResult> SetLocationItem([FromBody] LocationTableItem LocationItem)
        {
            if (LocationItem == null)
            {
                return BadRequest(new { message = "empty LocationItem" });
            }

            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/SetLocationTableItem", LocationItem);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLocationItem([FromBody] LocationTableItem locationItem)
        {
            if (locationItem == null)
            {
                return BadRequest(new { message = "empty locationItem" });
            }

            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/UpdateLocationTableItem", locationItem);
        }

        [HttpGet]
        public async Task<IActionResult> SalarySettingsExperienceLocations()
        {
            const string requestUrl = "http://rknet-server:1571/api/Edit/GetExperienceTable";
            var items = await LoadSalarySettingsItemsAsync<ExperienceTableItem>(requestUrl);

            return PartialView("_SalarySettingsExperienceLocations", items);
        }

        [HttpPost]
        public async Task<IActionResult> SetExperienceItem([FromBody] ExperienceTableItem ExperienceItem)
        {
            if (ExperienceItem == null)
            {
                return BadRequest(new { message = "empty ExperienceItem" });
            }

            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/SetExperienceTableItem", ExperienceItem);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateExperienceItem([FromBody] ExperienceTableItem experienceItem)
        {
            if (experienceItem == null)
            {
                return BadRequest(new { message = "empty experienceItem" });
            }

            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/UpdateExperienceTableItem", experienceItem);
        }

        [HttpGet]
        public async Task<IActionResult> SalarySettingsProductionLocations()
        {
            const string requestUrl = "http://rknet-server:1571/api/Edit/GetBaseTable";
            return PartialView("_SalarySettingsProductionLocations");
        }

        [HttpPost]
        public async Task<IActionResult> SetBaseTableItem([FromBody] BaseTableItem baseTableItem)
        {
            if (baseTableItem == null)
            {
                return BadRequest(new { message = "empty baseTableItem" });
            }

            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/SetBaseTableItem", baseTableItem);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBaseTableItem([FromBody] BaseTableItem baseTableItem)
        {
            if (baseTableItem == null)
            {
                return BadRequest(new { message = "empty baseTableItem" });
            }

            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/UpdateBaseTableItem", baseTableItem);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteItem(int id)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.DeleteAsync($"http://rknet-server:1571/api/Edit/DeleteItem?id={id}");

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }

            return StatusCode((int)response.StatusCode, new
            {
                message = await response.Content.ReadAsStringAsync()
            });
        }

        private async Task<IActionResult> ProxyPostAsync<T>(string requestUrl, T payload)
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(requestUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                return Ok();
            }

            return StatusCode((int)response.StatusCode, new
            {
                message = await response.Content.ReadAsStringAsync()
            });
        }

        private async Task<List<T>> LoadSalarySettingsItemsAsync<T>(string requestUrl)
        {
            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.SalarySettingsTabLoadError = $"Не удалось загрузить данные (HTTP {(int)response.StatusCode})";
                    return new List<T>();
                }

                return await response.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
            }
            catch
            {
                ViewBag.SalarySettingsTabLoadError = "Ошибка обращения к сервису данных";
                return new List<T>();
            }
        }

        public class SalarySettingsBaseTable 
        {
            public List<BaseTableItem> Items { get; set; }
            public List<JobTitle> jobTitles { get; set; }
            public List<Portal.Models.MSSQL.Location.Location> Locations { get; set; }
        }

        public class BaseTableItem
        {
            public int? RuleId { get; set; }
            public Models.MSSQL.Personality.JobTitle? MainJob { get; set; }
            public Models.MSSQL.Personality.JobTitle? RealJob { get; set; }
            public string BSM { get; set; }
            public string BAK { get; set; }
            public string EXK { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
            public bool PartTimer { get; set; }
        }

        public class LocationTableItem
        {
            public int RuleId { get; set; }
            public Models.MSSQL.Location.Location Location { get; set; }
            public int BAM { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
        }

        public class ExperienceTableItem
        {
            public int RuleId { get; set; }
            public int? EXPmin { get; set; }
            public int? EXPmax { get; set; }
            public int EXM { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
        }

    }
}
