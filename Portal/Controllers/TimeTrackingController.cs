using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;
using System.Security.Claims;
using Portal.Models.Calculator;
using Portal.Models;
using Microsoft.CodeAnalysis;

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
        public IActionResult TrackingData(string begin, string end) 
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
                beginDateTime = DateTime.ParseExact(begin, "dd.MM.yyyy", null);
                endDateTime = DateTime.ParseExact(begin, "dd.MM.yyyy", null);
            }
            List<DateTime> dateList = new List<DateTime>();
            for (DateTime i = beginDateTime; i <= endDateTime; i = i.AddDays(1))
            {
                dateList.Add(i);
            }
            string userLogin = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.WindowsAccountName).Value;
            RKNet_Model.Account.User user = db.Users.Include(c => c.TTs.Where(d => d.CloseDate == null && d.Type != null && d.Type.Id != 3))
                                                    .FirstOrDefault(c => c.Login == userLogin);
            foreach (var TT in user.TTs)
            {
                Portal.Models.MSSQL.Location.Location location = dbSql.Locations.FirstOrDefault(c => TT.Restaurant_Sifr == c.RKCode);
                TTData tTData = new TTData();
                tTData.Location = location;
                tTData.DateDatas = new();               
                foreach (var date in dateList)
                {
                    DateData dateData = new DateData();
                    dateData.Date = date;
                    dateData.TimeSheets = dbSql.TimeSheets.Include(c => c.Personality)
                                                          .Include(c => c.Location)
                                                          .Include(c => c.JobTitle)
                                                          .Where(c => c.Location == location && c.Begin > date && c.Begin < date.AddDays(1))
                                                          .ToList();
                    tTData.DateDatas.Add(dateData);
                }
                trackingDataModel.TTDatas.Add(tTData);
            }

            return PartialView(trackingDataModel);
        }
     
        public IActionResult TrackingDataTable() 
        {
            return PartialView();
        }
    } 
}
