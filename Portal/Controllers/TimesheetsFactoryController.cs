using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Location;
using RKNet_Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using Portal.Models.MSSQL.PersonalityVersions;
using Newtonsoft.Json;
using Portal.Models.JsonModels;
using static Portal.Controllers.TimesheetsFactoryController;
using Portal.Models.MSSQL.Personality;
using System.Globalization;

namespace Portal.Controllers
{
    public class TimesheetsFactoryController : Controller
    {
        private DB.MSSQLDBContext dbSql;
        private IHttpClientFactory _httpClientFactory;
        private DB.SQLiteDBContext db;
        public TimesheetsFactoryController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IHttpClientFactory _httpClientFactoryConnect)
        {
            dbSql = dbSqlContext;
            db = context;
            _httpClientFactory = _httpClientFactoryConnect;
        }

        public class TimeTrackingFactoryInterface
        {
            public List<TimeTrackingFactoryForPerson> TimesheetsFactory { get; set; }
            public List<LocationVersions> LocationVersions { get; set; }
            public DateTime CurrentDate { get; set; }
            public string Location { get; set; }
        }

        public class TimeTrackingFactoryForPerson
        {
            public List<TimeSheetsFactory> TimesheetsFactory { get; set; }
            public double[] ArrayHours { get; set; }
            public PersonalityVersion PersonalityVersion { get; set; }
            public Portal.Models.MSSQL.Personality.JobTitle JobTitle { get; set; }
        }

        

        public IActionResult TimesheetsMain(string loc = "", int month = 0, int year = 0)
        {
            var timeTrackingFactoryInterface = new TimeTrackingFactoryInterface();

            try
            {
                // ≈сли дата не указана, то ставим текущую
                if (month == 0 || year == 0)
                {
                    month = DateTime.Now.Month;
                    year = DateTime.Now.Year;
                }
                timeTrackingFactoryInterface.CurrentDate = new DateTime(year, month, 1);

                // ¬ысчитываем границы дат (начало и конец мес€ца) + считаем дни в мес€це
                DateTime startDate = new DateTime(year, month, 1);
                DateTime endDate = new DateTime(year, month + 1, 1);
                int currentMonthDays = DateTime.DaysInMonth(year, month);

                // ѕолучаем текущего пользовател€ из сессии и провер€ем его в базе
                var userName = User.Identities.FirstOrDefault()?.Name;
                var user = db.Users.FirstOrDefault(x => x.Name == userName);
                var userId = user.Id;

                // ѕровер€ем прив€занные к пользователю точки (заводы и цеха)
                var bindingLocationToUsers = dbSql.BindingLocationToUsers.AsNoTracking().Where(x => x.UserID == userId).ToList();
                var locationVersions = new List<LocationVersions>();

                foreach (var item in bindingLocationToUsers)
                {
                    var locationVersion = dbSql.LocationVersions.Include(x => x.Location).Include(x => x.Location.LocationType).FirstOrDefault(x => x.Guid == item.LocationID);
                    if (locationVersion != null)
                    {
                        locationVersions.Add(locationVersion);
                    }
                }
                LocationVersions locVer;

                // ≈сли точка не передана, то берем первую подход€щую и по ней собираем информации далее
                if (loc == "")
                {
                    locVer = locationVersions.Where(x => x.Location.LocationType.Guid == Guid.Parse("80423E42-DC1E-4311-AD0B-08DCA4A09C33") || x.Location.LocationType.Guid == Guid.Parse("D3A0363D-2EC4-48E4-AD0C-08DCA4A09C33"))
                                      .FirstOrDefault();
                }else
                {
                    locVer = locationVersions.FirstOrDefault(x => x.Guid == Guid.Parse(loc));
                }
                timeTrackingFactoryInterface.LocationVersions = locationVersions;

                // ¬се сотрудники с указанной точки
                List<PersonalityVersion> personalityVersions = dbSql.PersonalityVersions.Where(x => x.Actual == 1)
                                                                                        .Where(x => x.Location.Guid == locVer.Location.Guid)
                                                                                        .Include(x => x.JobTitle)
                                                                                        .Include(x => x.Personalities)
                                                                                        .OrderBy(x => x.Surname)
                                                                                        .ToList();

                // ¬се записи по указанной точке
                List<TimeSheetsFactory> timesheets = dbSql.TimeSheetsFactory.Include(x => x.Location)
                                                                            .Where(x => x.Location.Guid == locVer.Guid)
                                                                            .Include(x => x.Personality)
                                                                            .ToList();

                // ”никальные сотрудники в запис€х
                List<TimeSheetsFactory> uniquePersonFromTimeSheetsFactory = timesheets.GroupBy(ts => ts.Personality.Guid)
                                                                                      .Select(group => group.First())
                                                                                      .ToList();

                foreach (var item in uniquePersonFromTimeSheetsFactory)
                {
                    if(!personalityVersions.Contains(item.Personality))
                    {
                        PersonalityVersion personalityVersion = dbSql.PersonalityVersions.Include(x => x.JobTitle)
                                                                                         .Include(x => x.Personalities)
                                                                                         .FirstOrDefault(x => x.Guid == item.Personality.Guid);
                        personalityVersions.Add(personalityVersion);
                    }
                }



                // Ќачинаем заполн€ть данные по каждому сотруднику
                List <TimeTrackingFactoryForPerson> timeTrackingFactoryForPersons = new List<TimeTrackingFactoryForPerson>();
                foreach (var item in personalityVersions)
                {
                    // —юда попадают все записи по одному сотруднику, даже если он отработал на разных должност€х в этот период
                    // —оздаютс€ группы по должност€м
                    var groupedTimeSheetsFactories = dbSql.TimeSheetsFactory.Where(x => x.Personality.Guid == item.Guid)
                                                                            .Where(x => x.Date >= startDate && x.Date <= endDate)
                                                                            .Include(x => x.Location)
                                                                            .Where(x => x.Location.Guid == locVer.Guid)
                                                                            .Include(x => x.JobTitle)
                                                                            .Include(x => x.Entity)
                                                                            .OrderBy(x => x.Date)
                                                                            .ToList()
                                                                            .GroupBy(x => x.JobTitle.Guid)
                                                                            .ToList();

                    

                    if (groupedTimeSheetsFactories.Count == 0)
                    {
                        TimeTrackingFactoryForPerson timeTrackingFactoryForPerson = new TimeTrackingFactoryForPerson();
                        double[] array = new double[currentMonthDays * 2];
                        timeTrackingFactoryForPerson.JobTitle = item.JobTitle;
                        timeTrackingFactoryForPerson.TimesheetsFactory = null;
                        timeTrackingFactoryForPerson.ArrayHours = array;
                        timeTrackingFactoryForPerson.PersonalityVersion = item;
                        timeTrackingFactoryForPersons.Add(timeTrackingFactoryForPerson);
                        continue;
                    }

                    foreach (var jobGroup in groupedTimeSheetsFactories)
                    {
                        List<TimeSheetsFactory> timeSheetsFactories = jobGroup.ToList();
                        TimeTrackingFactoryForPerson timeTrackingFactoryForPerson = new TimeTrackingFactoryForPerson();
                        double[] array = new double[currentMonthDays * 2];

                        timeSheetsFactories.ForEach(x =>
                        {
                            if(x.JobTitle.Guid == timeSheetsFactories[0].JobTitle.Guid)
                            {
                                if (x.PartDay == 0)
                                {
                                    array[(x.Date.Day * 2) - 1] = x.Hours;
                                }
                                else
                                {
                                    array[(x.Date.Day * 2) - 2] = x.Hours;
                                }
                            }
                        });

                        timeTrackingFactoryForPerson.JobTitle = timeSheetsFactories[0].JobTitle;
                        timeTrackingFactoryForPerson.TimesheetsFactory = timeSheetsFactories;
                        timeTrackingFactoryForPerson.ArrayHours = array;
                        timeTrackingFactoryForPerson.PersonalityVersion = item;
                        timeTrackingFactoryForPersons.Add(timeTrackingFactoryForPerson);

                    }
                }

                timeTrackingFactoryInterface.TimesheetsFactory = timeTrackingFactoryForPersons;
                timeTrackingFactoryInterface.Location = locVer.Guid.ToString();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error." + ex);
            }

            return PartialView(timeTrackingFactoryInterface);
        }

        public class SaveTimeSheets
        {
            public Dictionary<string, Record> Data { get; set; }
            public string Location { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
        }

        public class Record
        {
            public List<string> Entries { get; set; }
            public string Job { get; set; }
            public string Guid { get; set; }
        }

        public IActionResult TimesheetsSave(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                SaveTimeSheets jsonData = JsonConvert.DeserializeObject<SaveTimeSheets>(json);
                DateTime startDate = new DateTime(jsonData.Year, jsonData.Month, 1);
                DateTime endDate = new DateTime(jsonData.Year, jsonData.Month + 1, 1);
                foreach (var item in jsonData.Data)
                {
                    List<TimeSheetsFactory> timeSheetsFactoriesForDelete = dbSql.TimeSheetsFactory.Include(x => x.Personality)
                                                                                                  .Include(x => x.Entity)
                                                                                                  .Include(x => x.JobTitle)
                                                                                                  .Include(x => x.Location)
                                                                                                  .Where(x => x.Date >= startDate && x.Date <= endDate)
                                                                                                  .Where(x => x.Personality.Guid == Guid.Parse(item.Value.Guid))
                                                                                                  .Where(x => x.JobTitle.Guid == Guid.Parse(item.Value.Job))
                                                                                                  .Where(x => x.Location.Guid == Guid.Parse(jsonData.Location))
                                                                                                  .ToList();

                    // ”дал€ем все записи за выбранный мес€ц у выбранного человека
                    foreach (var timeSheet in timeSheetsFactoriesForDelete)
                    {
                        dbSql.TimeSheetsFactory.Remove(timeSheet);
                        dbSql.SaveChanges();
                    }

                    for (var i = 0; i < item.Value.Entries.Count; i++)
                    {
                        if (item.Value.Entries[i] == "")
                        {
                            continue;
                        }
                        TimeSheetsFactory timeSheetsFactoryForAdd = new TimeSheetsFactory();
                        timeSheetsFactoryForAdd.Location = dbSql.LocationVersions.FirstOrDefault(x => x.Guid == Guid.Parse(jsonData.Location));
                        timeSheetsFactoryForAdd.Personality = dbSql.PersonalityVersions.Include(x => x.Entity)
                                                                                       .Include(x => x.JobTitle)
                                                                                       .FirstOrDefault(x => x.Guid == Guid.Parse(item.Value.Guid));

                        var currentDay = i;

                        timeSheetsFactoryForAdd.Entity = timeSheetsFactoryForAdd.Personality.Entity;
                        timeSheetsFactoryForAdd.Date = new DateTime(jsonData.Year, jsonData.Month, (int)Math.Ceiling((currentDay + 1)/ 2.0));
                        timeSheetsFactoryForAdd.JobTitle = dbSql.JobTitles.FirstOrDefault(x => x.Guid == Guid.Parse(item.Value.Job));

                        // ¬ыбор половины дн€ (1 = день, 0 = ночь)
                        if ((i + 1) % 2 != 0)
                        {
                            timeSheetsFactoryForAdd.PartDay = 1;
                        }
                        else
                        {
                            timeSheetsFactoryForAdd.PartDay = 0;
                        }
                        timeSheetsFactoryForAdd.Hours = double.Parse(item.Value.Entries[i], CultureInfo.InvariantCulture);

                        // ƒобавление записи на каждое число
                        dbSql.TimeSheetsFactory.Add(timeSheetsFactoryForAdd);
                        dbSql.SaveChanges();
                    }
                }
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result.ErrorMessage);
            }
        }

        public IActionResult GetJobList()
        {
            List<JobTitle> jobTitles = dbSql.JobTitles.ToList();
            return Json(jobTitles);
        }

        public IActionResult GetNameList()
        {
            List<PersonalityVersion> personalityVersions = dbSql.PersonalityVersions.Where(x => x.Actual == 1).ToList();
            return Json(personalityVersions);
        }
        
    }
}


