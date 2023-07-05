using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.Personality;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Portal.Controllers
{
    public class PersonalityController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        public PersonalityController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }

        public IActionResult PersonalityTable(int showUnActual)
        {
            Models.PersonalityModel model = new Models.PersonalityModel();
            model.Personalities = dbSql.Personalities.Include(c => c.JobTitle)
                                                             .Include(c => c.Location)
                                                             .Include(c => c.Schedule)
                                                             .Where(c => c.Actual > 0)
                                                             .ToList();
            if (showUnActual == 0) { model.Personalities = model.Personalities.Where(c => c.Actual != 0).ToList(); }            
            return PartialView(model);
        }

        public IActionResult PersonalityEdit(string typeGuid)
        {
            Models.PersonalityEditModel model = new Models.PersonalityEditModel();
            if (typeGuid != null)
            {
                model.Personality = dbSql.Personalities.Include(c => c.JobTitle)
                                                        .Include(c => c.Location)
                                                        .Include(c => c.Schedule)
                                                        .FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            }
            model.Schedules = dbSql.Schedules.ToList();
            model.Locations = dbSql.Locations.ToList();
            model.JobTitles = dbSql.JobTitles.ToList();
            return PartialView(model);
        }
    }
}
