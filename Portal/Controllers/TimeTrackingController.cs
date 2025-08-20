using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models;
using Portal.Models.JsonModels;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Portal.Controllers
{
    [Authorize]
    public class TimeTrackingController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        public TimeTrackingController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }
        public IActionResult TrackingDataTable(string begin, string end, string locationguid)
        {
            DateTime beginDateTime = new();
            DateTime endDateTime = new();
            TrackingDataModel trackingDataModel = new TrackingDataModel();
            trackingDataModel.TTDatas = new();
            if (begin == null || end == null)
            {
                beginDateTime = DateTime.Now.Date;
                endDateTime = DateTime.Now.Date;
            }
            else
            {
                beginDateTime = DateTime.ParseExact(begin, "yyyy-MM-dd", null);
                endDateTime = DateTime.ParseExact(end, "yyyy-MM-dd", null);
            }
            trackingDataModel.Begin = beginDateTime;
            trackingDataModel.End = endDateTime;
            List<DateTime> dateList = new List<DateTime>();
            for (DateTime i = beginDateTime; i <= endDateTime; i = i.AddDays(1))
            {
                dateList.Add(i);
            }
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs.Where(d => d.CloseDate == null && d.Type != null && d.Type.Id != 3))
                                                    .FirstOrDefault(c => c.Login == userLogin);
            trackingDataModel.Location = new();
            foreach (var tt in user.TTs)
            {
                var location = dbSql.Locations.FirstOrDefault(c => c.RKCode == tt.Restaurant_Sifr);
                if (location == null) { continue; }
                trackingDataModel.Location.Add(location);
            }
            List<RKNet_Model.TT.TT> tts = new();
            if (!string.IsNullOrEmpty(locationguid) && locationguid != "0")
            {
                Portal.Models.MSSQL.Location.Location location = dbSql.Locations.FirstOrDefault(c => c.Guid == Guid.Parse(locationguid));
                RKNet_Model.TT.TT tt = db.TTs.FirstOrDefault(c => c.Restaurant_Sifr == location.RKCode);
                tts.Add(tt);
            }
            else
            {
                tts = user.TTs;
            }
            foreach (var TT in tts)
            {
                Portal.Models.MSSQL.Location.Location location = dbSql.Locations.FirstOrDefault(c => TT.Restaurant_Sifr == c.RKCode);
                if (location == null) { continue; }
                TTData tTData = new TTData();
                tTData.Location = location;
                tTData.DateDatas = new();
                foreach (var date in dateList)
                {
                    DateData dateData = new DateData();

                    dateData.Date = date;
                    dateData.TimeSheets = dbSql.TimeSheets.Include(c => c.Personalities)
                                                          .Include(c => c.Location)
                                                          .Include(c => c.JobTitle)
                                                          .Where(c => c.Location == location && c.Begin >= date && c.Begin < date.AddDays(1))
                                                          .ToList();

                    dateData.WorkingSlots = dbSql.WorkingSlots.Include(c => c.Personalities)
                                                          .Include(c => c.Locations)
                                                          .Include(c => c.JobTitles)
                                                          .Where(c => c.Locations == location && c.Begin >= date && c.Begin < date.AddDays(1))
                                                          .ToList();

                    dateData.Hours = CalculateHours(dateData.TimeSheets, dateData.WorkingSlots);

                    tTData.DateDatas.Add(dateData);
                }
                trackingDataModel.TTDatas.Add(tTData);
            }

            return PartialView(trackingDataModel);
        }
        public double CalculateHours(List<TimeSheet> timeSheets, List<WorkingSlots> workingSlots)
        {
            double totalHours = 0;


            void AddHours(DateTime begin, DateTime end)
            {
                TimeSpan duration = end - begin;
                if (duration < TimeSpan.Zero)
                {
                    duration = TimeSpan.FromHours(24) - begin.TimeOfDay + end.TimeOfDay;
                }
                totalHours += duration.TotalHours;
            }

            foreach (var item in timeSheets)
            {
                AddHours(item.Begin, item.End);
            }

            foreach (var item in workingSlots)
            {
                AddHours(item.Begin, item.End);
            }

            return totalHours;
        }
        public IActionResult TrackingData()
        {
            return PartialView();
        }
        public IActionResult TrackingDataEdit(string stringDate, string locationGuid)
        {
            DateTime date = DateTime.ParseExact(stringDate, "dd-MM-yyyy", null);

            List<PersonalityVersion> pervers = dbSql.PersonalityVersions.Include(c => c.Personalities)
                                                                        .Include(c => c.JobTitle)
                                                                        .Include(c => c.Schedule)
                                                                        .Include(c => c.Location)
                                                                        .Where(c => c.VersionStartDate <= date && (c.VersionEndDate == null || c.VersionEndDate >= date))
                                                                        .ToList();

            TTData tTData = new TTData();
            Portal.Models.MSSQL.Location.Location location = dbSql.Locations.FirstOrDefault(c => c.Guid == Guid.Parse(locationGuid));
            DateData dateData = new DateData();
            dateData.Date = date;

            dateData.TimeSheets = dbSql.TimeSheets.Include(c => c.Personalities)
                                                  .Include(c => c.JobTitle)
                                                  .Where(c => c.Begin.Date == date && c.Location.Guid == location.Guid)
                                                  .ToList();
            dateData.WorkingSlots = dbSql.WorkingSlots.Include(c => c.Personalities)
                                                  .Include(c => c.JobTitles)
                                                  .Where(c => c.Begin.Date == date && c.Locations.Guid == location.Guid)
                                                  .ToList();

            tTData.Location = location;
            tTData.DateDatas = new List<DateData> { dateData };
            TrackingDataEditModel trackingDataEditModel = new TrackingDataEditModel();
            trackingDataEditModel.TTData = tTData;
            trackingDataEditModel.Hours = CalculateHours(dateData.TimeSheets, dateData.WorkingSlots);
            trackingDataEditModel.Personalities = dbSql.Personalities.ToList();
            trackingDataEditModel.PersonalityVersions = pervers;
            trackingDataEditModel.JobTitles = dbSql.JobTitles.ToList();

            return PartialView(trackingDataEditModel);
        }
        public IActionResult TimeTrackingAdd(string json, bool editChart = false)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                TimeSheetJsonModel timeSheetJsonModel = JsonConvert.DeserializeObject<TimeSheetJsonModel>(json);
                List<TimeSheet> addedTimeSheets = new List<TimeSheet>();
                foreach (var TimeSheet in timeSheetJsonModel.TimeSheetJson)
                {
                    Models.MSSQL.TimeSheet TimeSheets = new TimeSheet();
                    TimeSheets.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == TimeSheet.Personalities);
                    TimeSheets.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == TimeSheet.Location);
                    TimeSheets.JobTitle = dbSql.JobTitles.FirstOrDefault(c => c.Guid == TimeSheet.JobTitle);
                    TimeSheets.Begin = TimeSheet.Begin;
                    TimeSheets.End = TimeSheet.End;
                    addedTimeSheets.Add(TimeSheets);
                }

                List<TimeSheet> removedTimeSheets = new List<TimeSheet>();

                if (!editChart)
                {
                    removedTimeSheets = dbSql.TimeSheets.Include(c => c.Location)
                                                                    .Where(c => c.Location.Guid == timeSheetJsonModel.Location && c.Begin.Date == timeSheetJsonModel.Date)
                                                                    .ToList();
                }


                List<Guid> selectedPersonalityGuid = timeSheetJsonModel.TimeSheetJson.Select(c => c.Personalities).Distinct().ToList();
                List<TimeSheet> checkingTimeSeets = new List<TimeSheet>();

                foreach (var personality in selectedPersonalityGuid)
                {
                    checkingTimeSeets.AddRange(dbSql.TimeSheets.Include(c => c.Personalities)
                                                               .Include(c => c.Location)
                                                               .Where(c => c.Personalities.Guid == personality && c.Begin.Date == timeSheetJsonModel.Date)
                                                               .ToList());
                }
                checkingTimeSeets = checkingTimeSeets.Where(c => !removedTimeSheets.Any(d => d.Guid == c.Guid)).ToList();
                checkingTimeSeets.AddRange(addedTimeSheets);

                string error = CheckTimesheetsForСoincidenceTime(checkingTimeSeets, selectedPersonalityGuid);
                if (error != "")
                {
                    return new ObjectResult(error);
                }

                if (removedTimeSheets != null)
                {
                    foreach (var item in removedTimeSheets)
                    {
                        dbSql.TimeSheetsLogs.Where(x => x.TimeSheets.Guid == item.Guid).ToList().ForEach(x => x.TimeSheets = null);
                    }
                }

                dbSql.TimeSheets.RemoveRange(removedTimeSheets);
                dbSql.AddRange(addedTimeSheets);

                List<WorkingSlots> addedWorkingSlots = new List<WorkingSlots>();
                List<WorkingSlots> removedWorkingSlots = new List<WorkingSlots>();
                List<int> ExistingSlots = new List<int>();
                foreach (var WorkSlot in timeSheetJsonModel.WorkSlotsJson)
                {
                    // Если слот новый
                    if (WorkSlot.Id == 0)
                    {
                        Models.MSSQL.WorkingSlots newWorkSlots = new WorkingSlots();
                        newWorkSlots.Locations = dbSql.Locations.FirstOrDefault(c => c.Guid == timeSheetJsonModel.Location);
                        newWorkSlots.JobTitles = dbSql.JobTitles.FirstOrDefault(c => c.Guid == WorkSlot.JobTitle);
                        newWorkSlots.Begin = WorkSlot.Begin;
                        newWorkSlots.End = WorkSlot.End;
                        newWorkSlots.Status = 0;
                        addedWorkingSlots.Add(newWorkSlots);
                        continue;
                    }
                    Models.MSSQL.WorkingSlots WorkSlotDB = dbSql.WorkingSlots.FirstOrDefault(c => c.Id == WorkSlot.Id);
                    if (WorkSlotDB is null)
                    {
                        return BadRequest("Пользователь отсутствует в БД");
                    }
                    ExistingSlots.Add(WorkSlotDB.Id);
                    //Если пользователь имеет право изменять слот в любое время
                    if (User.IsInRole("time_tracking_administrator"))
                    {
                        WorkSlotDB.JobTitles = dbSql.JobTitles.FirstOrDefault(c => c.Guid == WorkSlot.JobTitle);
                        WorkSlotDB.Begin = WorkSlot.Begin;
                        WorkSlotDB.End = WorkSlot.End;
                        WorkSlotDB.Status = WorkSlot.Status;
                        dbSql.WorkingSlots.Update(WorkSlotDB);
                        continue;
                    }
                    //Если слот не занят
                    if (WorkSlotDB.Status == 0)
                    {
                        WorkSlotDB.JobTitles = dbSql.JobTitles.FirstOrDefault(c => c.Guid == WorkSlot.JobTitle);
                        WorkSlotDB.Begin = WorkSlot.Begin;
                        WorkSlotDB.End = WorkSlot.End;
                        dbSql.WorkingSlots.Update(WorkSlotDB);
                        continue;
                    }
                    //Если занят
                    if (WorkSlotDB.Status == 1 && DateTime.Now > WorkSlot.End)
                    {
                        WorkSlotDB.Begin = WorkSlot.Begin;
                        WorkSlotDB.End = WorkSlot.End;
                        WorkSlotDB.Status = WorkSlot.Status;
                        dbSql.WorkingSlots.Update(WorkSlotDB);
                        continue;
                    }
                }
                removedWorkingSlots = dbSql.WorkingSlots.Include(c => c.Locations)
                                                                    .Where(c => c.Locations.Guid == timeSheetJsonModel.Location &&
                                                                           c.Begin.Date == timeSheetJsonModel.Date &&
                                                                           !ExistingSlots.Contains(c.Id))
                                                                    .ToList();

                dbSql.WorkingSlots.RemoveRange(removedWorkingSlots);
                dbSql.AddRange(addedWorkingSlots);
                dbSql.SaveChanges();

                // Если есть добавленные слоты, возвращаем Guid первого
                if (addedTimeSheets.Any())
                {
                    var firstAddedSlotGuid = addedTimeSheets.First().Guid;
                    result.Ok = true;
                    result.Data = firstAddedSlotGuid.ToString();
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
        private string CheckTimesheetsForСoincidenceTime(List<TimeSheet> checkingTimeSeets, List<Guid> selectedPersonalityGuid)
        {
            foreach (var personality in selectedPersonalityGuid)
            {
                List<TimeSheet> timeSheets = checkingTimeSeets.Where(c => c.Personalities.Guid == personality).OrderBy(c => c.Begin).ToList();
                DateTime selectedDatetime = new();
                for (int i = 0; i < timeSheets.Count; i++)
                {
                    if (selectedDatetime > timeSheets[i].Begin)
                    {
                        if (timeSheets[i]?.JobTitle?.Guid == Guid.Parse("880A47B4-9A4F-41DF-ACA5-DB17FFE0635B")) { return ""; }

                        return ($"Конфликт рабочего времени!\nСотрудник: {timeSheets[i].Personalities.Name}\n" +
                                $"Уже активен на точке: {checkingTimeSeets[0].Location.Name}\n" +
                                $"Временной слот {timeSheets[0].Begin} - {timeSheets[0].End}");
                    }
                    selectedDatetime = timeSheets[i].End;
                }
            }
            return "";
        }
        public IActionResult TrackingChart(string locationGuid, string date)
        {
            TrackingChartData trackingChartData = new TrackingChartData();
            List<DataForCharts> mainData = new List<DataForCharts>();

            // нужна дата формата 21-05-2025
            if (date == null)
            {
                date = DateTime.Now.ToString();
            }

            trackingChartData.selectedDate = date;

            var now = DateTime.Parse(date);
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            // Добавляем тт, которые привзяаны к пользователю
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs.Where(d => d.CloseDate == null && d.Type != null && d.Type.Id != 3))
                                                    .FirstOrDefault(c => c.Login == userLogin);
            trackingChartData.locations = new();
            foreach (var tt in user.TTs)
            {
                var location = dbSql.Locations.FirstOrDefault(c => c.RKCode == tt.Restaurant_Sifr);
                if (location == null) { continue; }
                trackingChartData.locations.Add(location);
            }

            if (locationGuid == null)
            {
                locationGuid = trackingChartData.locations.OrderBy(x => x.Name).FirstOrDefault().Guid.ToString();
            }

            trackingChartData.selectedLocation = locationGuid;

            // Берем все смены, по этой ТТ за месяц
            List<TimeSheet> timeSheets = dbSql.TimeSheets.Include(x => x.Location)
                                                         .Include(x => x.Personalities)
                                                         .Include(x => x.JobTitle)
                                                         //.Where(x => x.Location.Guid == Guid.Parse(locationGuid))
                                                         .Where(x => x.Begin >= firstDayOfMonth && x.End <= lastDayOfMonth)
                                                         .ToList();

            // Список пользователей (которые работали в этом месяце)
            var personalityGuids = timeSheets.Where(x => x.Location.Guid == Guid.Parse(locationGuid)).Select(p => p.Personalities.Guid).ToList();

            List<PersonalityVersion> personalities = dbSql.PersonalityVersions.Include(x => x.Location)
                                                                              .Include(x => x.Personalities)
                                                                              .Where(x => x.Actual == 1)
                                                                              .Where(x => personalityGuids.Contains(x.Personalities.Guid))
                                                                              .ToList();

            List<PersonalityVersion> personalitiesFromThisTT = dbSql.PersonalityVersions.Include(x => x.Location)
                                                                                        .Include(x => x.Personalities)
                                                                                        .Where(x => x.Location.Guid == Guid.Parse(locationGuid))
                                                                                        .Where(x => x.Actual == 1)
                                                                                        .Where(x => !personalities.Contains(x))
                                                                                        .ToList();

            personalities.AddRange(personalitiesFromThisTT);

            foreach (var person in personalities)
            {

                List<TimeSheet> timesheets = timeSheets.Where(x => x.Personalities.Guid == person.Personalities.Guid)
                                                       .ToList();

                DataForCharts data = new DataForCharts
                {
                    person = person,
                    timesheets = timesheets,
                    shedules = Enumerable.Range(0, lastDayOfMonth.Day)
                             .Select(_ => new sheduleItem())
                             .ToList()
                };


                double totalHours = 0;

                foreach (var timesheet in timesheets)
                {
                    var duration = (timesheet.End - timesheet.Begin).TotalHours;
                    totalHours += duration;

                    sheduleItem sheduleItem = new();
                    //sheduleItem.duration = duration;
                    sheduleItem.duration = timesheet.Begin.ToString("HH:mm") + " " + timesheet.End.ToString("HH:mm");
                    sheduleItem.location = timesheet.Location.Guid.ToString();
                    sheduleItem.locationName = timesheet.Location.Name.ToString();

                    int day = timesheet.Begin.Day - 1;

                    // traitorStatus обозначает "верность" сотрудника к своей ТТ
                    // 0 - сотрудник посещал только свою ТТ
                    // 1 - сотрудник посещал только другую ТТ
                    // 2 - сотрудник посещал и свою и чужую ТТ


                    if (sheduleItem.jobTitle == null)
                    {
                        sheduleItem.jobTitle = new List<string>();
                    }

                    if (timesheet?.JobTitle?.Name == null)
                    {
                        var jobname = dbSql.JobTitles.FirstOrDefault().Name;
                        sheduleItem.jobTitle.Add(jobname);
                    }
                    else
                    {
                        sheduleItem.jobTitle.Add(timesheet?.JobTitle?.Name);
                    }



                    // Если это первое заполнение этой ячейки, то просто проверим на соответствие ТТ
                    if (data.shedules[day].location == null)
                    {
                        data.shedules[day].jobTitle = new List<string>
                        {
                            timesheet?.JobTitle?.Name
                        };

                        // Ставим предателя, если смена была не на родной точке
                        if (sheduleItem.location != locationGuid)
                        {
                            sheduleItem.traitorStatus = 1;
                        }
                        else
                        {
                            sheduleItem.traitorStatus = 0;
                        }

                        data.shedules[day] = sheduleItem;
                    }
                    else
                    {

                        // Если это не первая ячейка, то проверим на соответствие родной точки 
                        data.shedules[day].duration += " " + timesheet.Begin.ToString("HH:mm") + " " + timesheet.End.ToString("HH:mm");
                        data.shedules[day].jobTitle.Add(timesheet?.JobTitle?.Name);

                        if (sheduleItem.location == locationGuid)
                        {
                            // Если запись уже не первая и статус (ходил только на свою || ходил и туда и сюда), то ставим  ходил и туда и сюда

                            if (data.shedules[day].traitorStatus == 0)
                            {
                                data.shedules[day].traitorStatus = 0;
                            }
                            else
                            {
                                data.shedules[day].traitorStatus = 2;
                            }

                        }
                        else
                        {
                            // Если запись не первая и статус (ходил только на чужую || ходил и туда и сюда), то ставим ходил и туда и сюда
                            if (data.shedules[day].traitorStatus == 0)
                            {
                                data.shedules[day].traitorStatus = 2;
                            }
                            else
                            {
                                data.shedules[day].traitorStatus = 1;
                            }
                            // если запись не первая и статус ходил только на свою, то оставляем его
                        }
                    }
                }



                data.Hours = totalHours;

                mainData.Add(data);
            }

            var workingSlots = dbSql.WorkingSlots.Include(x => x.Locations)
                                                 .Include(x => x.Personalities)
                                                 .Where(x => x.Begin >= firstDayOfMonth && x.End <= lastDayOfMonth)
                                                 .Where(x => x.Locations.Guid == Guid.Parse(locationGuid))
                                                 .AsEnumerable()
                                                 .GroupBy(x => x.Personalities == null ? null : x.Personalities)
                                                 .ToList();

            List<DataForCharts> exchangeData = new List<DataForCharts>();

            foreach(var item in workingSlots)
            {
                if(item.Key == null)
                {
                    DataForCharts data = new DataForCharts
                    {
                        person = null,
                        timesheets = null,
                        shedules = Enumerable.Range(0, lastDayOfMonth.Day)
                            .Select(_ => new sheduleItem())
                            .ToList()
                    };

                    foreach (var exch in item)
                    {
                        int day = exch.Begin.Day - 1;
                        sheduleItem sheduleItem = new();
                        sheduleItem.location = exch.Locations.Guid.ToString();
                        sheduleItem.locationName = exch.Locations.Name;
                        sheduleItem.traitorStatus = 0;
                        List<string> jobs = new List<string>();
                        jobs.Add(exch.JobTitles.Name);
                        sheduleItem.duration = exch.Begin.ToString("HH:mm") + " " + exch.End.ToString("HH:mm");
                        sheduleItem.jobTitle = jobs;

                        data.shedules[day] = sheduleItem;
                    }

                    exchangeData.Add(data);
                } else
                {
                    PersonalityVersion person = dbSql.PersonalityVersions.Include(x => x.Personalities).FirstOrDefault(x => x.Personalities.Guid == item.FirstOrDefault().Personalities.Guid);
                    DataForCharts data = new DataForCharts
                    {
                        person = person,
                        timesheets = null,
                        shedules = Enumerable.Range(0, lastDayOfMonth.Day)
                            .Select(_ => new sheduleItem())
                            .ToList()
                    };

                    foreach (var exch in item)
                    {
                        int day = exch.Begin.Day - 1;
                        sheduleItem sheduleItem = new();
                        sheduleItem.location = exch.Locations.Guid.ToString();
                        sheduleItem.locationName = exch.Locations.Name;
                        sheduleItem.traitorStatus = 0;
                        List<string> jobs = new List<string>();
                        jobs.Add(exch.JobTitles.Name);
                        sheduleItem.jobTitle = jobs;
                        sheduleItem.duration = exch.Begin.ToString("HH:mm") + " " + exch.End.ToString("HH:mm");

                        data.shedules[day] = sheduleItem;
                    }

                    exchangeData.Add(data);
                }

                
            }

            trackingChartData.ExchangeDataForCharts = exchangeData;

            trackingChartData.DataForCharts = mainData;

            List<JobTitle> jobTitles = dbSql.JobTitles.ToList();

            trackingChartData.jobTitles = jobTitles;



            return PartialView(trackingChartData);
        }
        public List<Schedule> GetSheduleTime(string guid)
        {
            List<Schedule> times = dbSql.Schedules.Where(x => x.JobTitleGuid == Guid.Parse(guid)).ToList();

            return times;
        }
        public IActionResult DeleteTimeSheet(string day, string person, string location)
        {
            var result = new RKNet_Model.Result<string>();

            DateTime date = DateTime.Parse(day);

            List<TimeSheet> timeSheet = dbSql.TimeSheets.Include(x => x.Personalities)
                .Include(x => x.Location)
                .Where(x => x.Personalities.Guid == Guid.Parse(person))
                .Where(x => x.Begin >= date && x.End <= date.AddHours(24))
                .Where(x => x.Location.Guid == Guid.Parse(location))
                .ToList();

            if (timeSheet != null)
            {
                foreach(var item in timeSheet)
                {
                    dbSql.TimeSheetsLogs.Where(x => x.TimeSheets.Guid == item.Guid).ToList().ForEach(x => x.TimeSheets = null);
                }

                dbSql.TimeSheets.RemoveRange(timeSheet);
                dbSql.SaveChanges();
            }

            return new ObjectResult(result);
        }
        public IActionResult TimeTrackingCancalled(string json)
        {
            var result = new RKNet_Model.Result<string>();

            TrackingCancalled trackingCancalledJsonModel = JsonConvert.DeserializeObject<TrackingCancalled>(json);

            TimeSheet timesheet = dbSql.TimeSheets.Include(x => x.Location)
                                                  .Include(x => x.Personalities)
                                                  .Include(x => x.JobTitle)
                                                  .FirstOrDefault(x => x.Guid == Guid.Parse(trackingCancalledJsonModel.Guid));

            TimeSheetsLogs timeSheetsLogs = new TimeSheetsLogs();

            timeSheetsLogs.Personalities = timesheet.Personalities;
            timeSheetsLogs.Location = timesheet.Location;
            timeSheetsLogs.JobTitle = timesheet.JobTitle;
            timeSheetsLogs.Begin = timesheet.Begin;
            timeSheetsLogs.End = timesheet.End;
            timeSheetsLogs.Text = trackingCancalledJsonModel.Text;

            if (trackingCancalledJsonModel.ToDelete == 1)
            {
                dbSql.TimeSheetsLogs.Where(x => x.TimeSheets.Guid == timesheet.Guid).ToList().ForEach(x => x.TimeSheets = null);

                timeSheetsLogs.TimeSheets = null;
                dbSql.TimeSheets.Remove(timesheet);
            }

            if(trackingCancalledJsonModel.ToDelete == 0)
            {
                timeSheetsLogs.TimeSheets = timesheet;
            }

            dbSql.TimeSheetsLogs.Add(timeSheetsLogs);
            dbSql.SaveChanges();

            return new ObjectResult(result);
        }

        public class TrackingCancalled
        {
            public string Guid { get; set; }
            public string Text { get; set; }
            public int ToDelete { get; set; }
        }

            public class TrackingChartData
            {
                public List<DataForCharts> DataForCharts { get; set; }
                public List<DataForCharts> ExchangeDataForCharts { get; set; }
                public List<Portal.Models.MSSQL.Location.Location> locations { get; set; }
                public string selectedLocation { get; set; }
                public string selectedDate { get; set; }
                public List<JobTitle> jobTitles { get; set; }
            }

            public class DataForCharts
            {
                public PersonalityVersion person { get; set; }
                public List<TimeSheet> timesheets { get; set; }
                public double Hours { get; set; }
                public List<sheduleItem> shedules { get; set; }
            }

            public class sheduleItem
            {
                //public double duration { get; set; }
                public string duration { get; set; }
                public string location { get; set; }
                public string locationName { get; set; }
                public int traitorStatus { get; set; }
                public List<string>? jobTitle { get; set; }
            }
    }
}
