using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models;
using Portal.Models.JsonModels;
using Portal.Models.MSSQL;
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
                    tTData.DateDatas.Add(dateData);
                }
                trackingDataModel.TTDatas.Add(tTData);
            }

            return PartialView(trackingDataModel);
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
            trackingDataEditModel.Personalities = dbSql.Personalities.ToList();
            trackingDataEditModel.PersonalityVersions = pervers;
            trackingDataEditModel.JobTitles = dbSql.JobTitles.ToList();
            return PartialView(trackingDataEditModel);
        }

        public IActionResult TimeTrackingAdd(string json)
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
                List<TimeSheet> removedTimeSheets = dbSql.TimeSheets.Include(c => c.Location)
                                                                    .Where(c => c.Location.Guid == timeSheetJsonModel.Location && c.Begin.Date == timeSheetJsonModel.Date)
                                                                    .ToList();
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
                        return ($"Конфликт рабочего времени!\nСотрудник: {timeSheets[i].Personalities.Name}\n" +
                                $"Уже активен на точке: {checkingTimeSeets[0].Location.Name}\n" +
                                $"Временной слот {timeSheets[0].Begin} - {timeSheets[0].End}");
                    }
                    selectedDatetime = timeSheets[i].End;
                }
            }
            return "";
        }

    }
}
