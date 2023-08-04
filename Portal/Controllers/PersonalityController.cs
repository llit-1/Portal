using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models.JsonModels;
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


        public IActionResult Personality()
        {
            return PartialView();
        }


        public IActionResult PersonalityTable(string showUnActual)
        {
            Models.PersonalityModel model = new Models.PersonalityModel();
            model.Personalities = dbSql.Personalities.Include(c => c.JobTitle)
                                                             .Include(c => c.Location)
                                                             .Include(c => c.Schedule)
                                                             .ToList();                                                            
            if (showUnActual == "0") { model.Personalities = model.Personalities.Where(c => c.Actual == 1).ToList(); }          
            return PartialView(model);
        }

        public IActionResult PersonalityEdit(string typeGuid)
        {
            Models.PersonalityEditModel model = new Models.PersonalityEditModel();
            if (typeGuid != "0")
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


        public IActionResult PersonalityAdd(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                PersonalityJson personalityJson = JsonConvert.DeserializeObject<PersonalityJson>(json);
                Models.MSSQL.Personality.Personality personality = new Personality();
                personality.Name = personalityJson.Name;
                personality.Surname = personalityJson.Surname;
                personality.Patronymic = personalityJson.Patronymic;
                personality.BirthDate = personalityJson.BirthDate;
                personality.JobTitle = dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle);
                personality.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                personality.HireDate = personalityJson.HireDate;
                personality.DismissalsDate = personalityJson.DismissalsDate;
                personality.Schedule = dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule);
                personality.Actual = personalityJson.Actual;
                dbSql.Add(personality);
                dbSql.SaveChanges();
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
        }

        public IActionResult PersonalityPut(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {               
                PersonalityJson personalityJson = JsonConvert.DeserializeObject<PersonalityJson>(json);
                Models.MSSQL.Personality.Personality personality = dbSql.Personalities.FirstOrDefault(c => c.Guid == personalityJson.Guid);
                personality.Name = personalityJson.Name;
                personality.Surname = personalityJson.Surname;
                personality.Patronymic = personalityJson.Patronymic;
                personality.BirthDate = personalityJson.BirthDate;
                personality.JobTitle = dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle);
                personality.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                personality.HireDate = personalityJson.HireDate;
                personality.DismissalsDate = personalityJson.DismissalsDate;
                personality.Schedule = dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule);
                personality.Actual = personalityJson.Actual;
                dbSql.SaveChanges();
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
        }

        public IActionResult PersonalityVersions()
        {
            Models.PersonalityModel model = new Models.PersonalityModel();
            model.Personalities = dbSql.Personalities.Include(c => c.JobTitle)
                                                             .Include(c => c.Location)
                                                             .Include(c => c.Schedule)
                                                             .ToList();
            return PartialView(model);
        }

    }
}
