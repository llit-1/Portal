using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [Authorize]
    public class SalaryController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        public SalaryController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
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
                Start = DateTime.Now.AddDays(-93);
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
                .OrderBy(x => x.Begin)
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
                    absence = x.Absence
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
    }
}
