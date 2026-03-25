using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.Personality;
using Portal.ViewModels.Salary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            if (Start == null && End == null && Month == null)
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
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetPersonMonthSalary(Guid? Person = null, int? Month = null, int? Year = null)
        {
            if (!Person.HasValue || Person.Value == Guid.Empty)
            {
                return BadRequest(new { message = "Не выбран сотрудник" });
            }
            if (!Month.HasValue || Month.Value < 1 || Month.Value > 12)
            {
                return BadRequest(new { message = "Некорректно выбран месяц" });
            }
            if (!Year.HasValue || Year.Value < 2000 || Year.Value > 3000)
            {
                return BadRequest(new { message = "Некорректно выбран год" });
            }
            string requestUrl = $"http://rknet-server:1571/api/Income/GetPersonIncome?person={Uri.EscapeDataString(Person.Value.ToString())}&month={Month.Value}&year={Year.Value}";
            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using HttpResponseMessage response = await httpClient.GetAsync(requestUrl);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorPayload = DeserializeApiPayload<ApiErrorResponse>(errorContent);
                    return StatusCode((int)response.StatusCode, new
                    {
                        message = errorPayload?.Message ?? "Не удалось загрузить расчет дохода"
                    });
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                var income = DeserializeApiPayload<PersonIncomeApiResponse>(responseContent);
                if (income == null)
                {
                    return StatusCode(502, new
                    {
                        message = "Сервис расчета дохода вернул пустой ответ"
                    });
                }
                var model = MapPersonIncomeToViewModel(Person.Value, Month.Value, Year.Value, income);
                return PartialView("_PersonMonthSalary", model);
            }
            catch
            {
                return StatusCode(502, new
                {
                    message = "Ошибка обращения к сервису расчета дохода"
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RefreshPersonMonthSalary(Guid? Person = null, int? Month = null, int? Year = null)
        {
            if (!Person.HasValue || Person.Value == Guid.Empty)
            {
                return BadRequest(new { message = "Не выбран сотрудник" });
            }
            if (!Month.HasValue || Month.Value < 1 || Month.Value > 12)
            {
                return BadRequest(new { message = "Некорректно выбран месяц" });
            }
            if (!Year.HasValue || Year.Value < 2000 || Year.Value > 3000)
            {
                return BadRequest(new { message = "Некорректно выбран год" });
            }
            string encodedPersonGuid = Uri.EscapeDataString(Person.Value.ToString());
            string query = $"month={Month.Value}&year={Year.Value}&personguid={encodedPersonGuid}";
            string timeSheetUrl = $"http://rknet-server:1571/api/CalculateSalary/Settimesheetpersonsalaries?{query}";
            string periodBonusUrl = $"http://rknet-server:1571/api/CalculateSalary/SetSalaryPeriodPersonCashBonus?{query}";
            try
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using var timeSheetContent = new ByteArrayContent(Array.Empty<byte>());
                using var periodBonusContent = new ByteArrayContent(Array.Empty<byte>());
                Task<HttpResponseMessage> timeSheetRequest = httpClient.PostAsync(timeSheetUrl, timeSheetContent);
                Task<HttpResponseMessage> periodBonusRequest = httpClient.PostAsync(periodBonusUrl, periodBonusContent);
                await Task.WhenAll(timeSheetRequest, periodBonusRequest);
                using HttpResponseMessage timeSheetResponse = await timeSheetRequest;
                using HttpResponseMessage periodBonusResponse = await periodBonusRequest;
                if (!timeSheetResponse.IsSuccessStatusCode)
                {
                    string errorContent = await timeSheetResponse.Content.ReadAsStringAsync();
                    var errorPayload = DeserializeApiPayload<ApiErrorResponse>(errorContent);
                    return StatusCode((int)timeSheetResponse.StatusCode, new
                    {
                        message = errorPayload?.Message ?? "Не удалось обновить начисления по табелю"
                    });
                }
                if (!periodBonusResponse.IsSuccessStatusCode)
                {
                    string errorContent = await periodBonusResponse.Content.ReadAsStringAsync();
                    var errorPayload = DeserializeApiPayload<ApiErrorResponse>(errorContent);
                    return StatusCode((int)periodBonusResponse.StatusCode, new
                    {
                        message = errorPayload?.Message ?? "Не удалось обновить периодические бонусы"
                    });
                }
                return Ok(new { success = true });
            }
            catch
            {
                return StatusCode(502, new
                {
                    message = "Ошибка обращения к сервису обновления зарплаты"
                });
            }
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
            const string requestUrl = "http://rknet-server:1571/api/Edit/GetProductionTable";
            var items = await LoadSalarySettingsItemsAsync<ProductionTableItem>(requestUrl);
            return PartialView("_SalarySettingsProductionLocations", items);
        }
        [HttpPost]
        public async Task<IActionResult> SetProductionTableItem([FromBody] ProductionTableItem productionTableItem)
        {
            if (productionTableItem == null)
            {
                return BadRequest(new { message = "empty productionTableItem" });
            }
            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/SetProductionTableItem", productionTableItem);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProductionTableItem([FromBody] ProductionTableItem productionTableItem)
        {
            if (productionTableItem == null)
            {
                return BadRequest(new { message = "empty productionTableItem" });
            }
            return await ProxyPostAsync("http://rknet-server:1571/api/Edit/UpdateProductionTableItem", productionTableItem);
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
        private PersonMonthSalaryViewModel MapPersonIncomeToViewModel(Guid personGuid, int month, int year, PersonIncomeApiResponse income)
        {
            var summaryCards = income.JobIncomes
                .SelectMany(x => x.IncomeItems ?? new List<PersonIncomeItemApiResponse>())
                .Where(x => x != null && x.Income > 0)
                .GroupBy(x => x.Name ?? "Прочие начисления")
                .Select(group => new PersonMonthSalarySummaryCardViewModel
                {
                    Title = group.Key,
                    Amount = group.Sum(x => x.Income),
                    Description = $"{FormatHours(group.Sum(x => x.Hours))} ч."
                })
                .ToList();
            summaryCards.AddRange(
                income.PeriodCashBonuses
                    .Where(x => x != null && x.CashBonus > 0)
                    .GroupBy(x => string.IsNullOrWhiteSpace(x.Name) ? "Периодический бонус" : x.Name)
                    .Select(group => new PersonMonthSalarySummaryCardViewModel
                    {
                        Title = group.Key,
                        Amount = group.Sum(x => x.CashBonus),
                        Description = $"{group.Count()} начисл."
                    }));
            var cardBackgrounds = new[]
            {
                "background: linear-gradient(24deg, #000000b5, #a4ffa4);",
                "background: linear-gradient(24deg, #000000b5, #a4b7ff);",
                "background: linear-gradient(24deg, #000000b5, #ffa4dc);",
                "background: linear-gradient(24deg, #000000b5, #a4ffea);"
            };
            var jobCards = income.JobIncomes
                .Select((jobIncome, index) => new PersonMonthSalaryJobCardViewModel
                {
                    Title = string.IsNullOrWhiteSpace(jobIncome.JobTitle) ? "Без должности" : jobIncome.JobTitle,
                    Subtitle = $"{jobIncome.TimesheetsCount} таб. / {jobIncome.IncomeItems.Count} начисл.",
                    TotalAmount = jobIncome.Income,
                    BackgroundStyle = cardBackgrounds[index % cardBackgrounds.Length],
                    Items = jobIncome.IncomeItems
                        .Where(x => x != null && x.Income > 0)
                        .Select(x => new PersonMonthSalaryJobIncomeItemViewModel
                        {
                            Name = x.Name,
                            Hours = x.Hours,
                            Amount = x.Income
                        })
                        .ToList()
                })
                .ToList();
            return new PersonMonthSalaryViewModel
            {
                PersonGuid = personGuid,
                Month = month,
                Year = year,
                MonthName = income.Month,
                PersonName = income.Name,
                TotalAmount = income.FinalIncome,
                SummaryCards = summaryCards,
                PeriodBonusItems = MapPeriodBonusItems(income.PeriodCashBonuses),
                JobCards = jobCards
            };
        }
        private string FormatHours(double hours)
        {
            var roundedHours = Math.Round(hours, 2);
            return roundedHours % 1 == 0
                ? ((int)roundedHours).ToString()
                : roundedHours.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
        }
        private T DeserializeApiPayload<T>(string payload) where T : class
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }
            try
            {
                return JsonSerializer.Deserialize<T>(payload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return null;
            }
        }
        private List<PersonMonthSalaryPeriodBonusViewModel> MapPeriodBonusItems(List<PeriodCashBonusApiResponse> periodCashBonuses)
        {
            return (periodCashBonuses ?? new List<PeriodCashBonusApiResponse>())
                .Where(x => x != null && x.CashBonus > 0)
                .Select(x => new PersonMonthSalaryPeriodBonusViewModel
                {
                    Name = x.Name,
                    Quantity = x.Quantity,
                    Amount = x.CashBonus,
                    Begin = x.Begin,
                    End = x.End
                })
                .ToList();
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
            public string PRK { get; set; }
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
        public class ProductionTableItem
        {
            public int RuleId { get; set; }
            public JobTitle TimesheetJobTitle { get; set; }
            public int TimesheetLong { get; set; }
            public int TimesheetCount { get; set; }
            public int PRM { get; set; }
            public DateTime Begin { get; set; }
            public DateTime End { get; set; }
        }
        private class ApiErrorResponse
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }
        }
        private class PersonIncomeApiResponse
        {
            [JsonPropertyName("jobIncomes")]
            public List<JobIncomeApiResponse> JobIncomes { get; set; } = new List<JobIncomeApiResponse>();
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("month")]
            public string Month { get; set; }
            [JsonPropertyName("year")]
            public int Year { get; set; }
            [JsonPropertyName("periodCashBonuses")]
            public List<PeriodCashBonusApiResponse> PeriodCashBonuses { get; set; } = new List<PeriodCashBonusApiResponse>();
            [JsonPropertyName("finalIncome")]
            public decimal FinalIncome { get; set; }
        }
        private class JobIncomeApiResponse
        {
            [JsonPropertyName("jobTitle")]
            public string JobTitle { get; set; }
            [JsonPropertyName("incomeItems")]
            public List<PersonIncomeItemApiResponse> IncomeItems { get; set; } = new List<PersonIncomeItemApiResponse>();
            [JsonPropertyName("timesheetsCount")]
            public int TimesheetsCount { get; set; }
            [JsonPropertyName("income")]
            public decimal Income { get; set; }
        }
        private class PersonIncomeItemApiResponse
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("incomeForHour")]
            public decimal IncomeForHour { get; set; }
            [JsonPropertyName("hours")]
            public double Hours { get; set; }
            [JsonPropertyName("income")]
            public decimal Income { get; set; }
        }
        private class PeriodCashBonusApiResponse
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("begin")]
            public DateTime Begin { get; set; }
            [JsonPropertyName("end")]
            public DateTime End { get; set; }
            [JsonPropertyName("cashBonus")]
            public decimal CashBonus { get; set; }
            [JsonPropertyName("quantity")]
            public decimal? Quantity { get; set; }
        }
    }
}
