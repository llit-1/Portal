using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;
using Portal.Models;

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
        public IActionResult TrackingDataTable() 
        {
            TrackingDataTableModel model = new();
            model.TTs = db.TTs.ToList();
            model.BeginDate = DateTime.Now;
            model.EndDate = DateTime.Now;
            return PartialView(model);
        }

        public IActionResult TrackingData() 
        {
            return PartialView();
        }

        public IActionResult TrackingDataEdit() 
        {
            return PartialView();
        }

    } 


    
}
