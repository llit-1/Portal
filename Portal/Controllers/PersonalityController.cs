using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Portal.Models;
using Portal.Models.JsonModels;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
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
            List<PersonalityModel> models = new List<PersonalityModel> ();
            List<Personality> personalities = dbSql.Personalities.ToList();
            foreach (var person in personalities)
            {
                if(showUnActual == "1")
                {
                    PersonalityVersion personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                                                     .Include(c => c.Location)
                                                                                     .Include(c => c.Personalities)
                                                                                     .Include(c => c.Schedule)
                                                                                     .Include(c => c.Entity)
                                                                                     .FirstOrDefault(c => c.Personalities.Guid == person.Guid && c.VersionEndDate == null);
                    if(personalityVersion != null)
                    {
                        PersonalityModel model = new PersonalityModel();
                        model.Personalities = person;
                        model.PersonalitiesVersions = personalityVersion;
                        models.Add(model);
                    }

                }
                else
                {
                    PersonalityVersion personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                                                     .Include(c => c.Location)
                                                                                     .Include(c => c.Personalities)
                                                                                     .Include(c => c.Schedule)
                                                                                     .Include(c => c.Entity)
                                                                                     .FirstOrDefault(c => c.Personalities.Guid == person.Guid && c.VersionEndDate == null && c.Actual == 1);
                    if (personalityVersion != null)
                    {
                        PersonalityModel model = new PersonalityModel();
                        model.Personalities = person;
                        model.PersonalitiesVersions = personalityVersion;
                        models.Add(model);
                    }
                }



            }


            return PartialView(models);
        }

        public IActionResult PersonalityEdit(string typeGuid, string newPerson)
        {
            Models.PersonalityEditModel model = new();
            if(newPerson == "2" && typeGuid != "0")
            {
                PersonalityVersion personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                                                  .Include(c => c.Location)
                                                                                  .Include(c => c.Schedule)
                                                                                  .Include(c => c.Personalities)
                                                                                  .Include(c => c.Entity)
                                                                                  .FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
                model.PersonalitiesVersions = personalityVersion;

                model.Personality = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            }
            else if(typeGuid != "0")
            {
                PersonalityVersion personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                                                  .Include(c => c.Location)
                                                                                  .Include(c => c.Schedule)
                                                                                  .Include(c => c.Personalities)
                                                                                  .Include(c => c.Entity)
                                                                                  .FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(typeGuid) && c.VersionEndDate == null);
                model.PersonalitiesVersions = personalityVersion;

                model.Personality = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            }
            model.NewPerson = newPerson;
            model.Schedules = dbSql.Schedules.ToList();
            model.Locations = dbSql.Locations.ToList();
            model.JobTitles = dbSql.JobTitles.ToList();
            model.Entity = dbSql.Entity.ToList();
            return PartialView(model);
        }


        public IActionResult PersonalityAdd(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                PersonalityJson personalityJson = JsonConvert.DeserializeObject<PersonalityJson>(json);
                if (personalityJson.NewPerson == "1")
                {
                    Models.MSSQL.Personality.Personality personality = new Personality();
                    personality.Name = personalityJson.Surname + " " + personalityJson.Name + " " + personalityJson.Patronymic;
                    personality.BirthDate = personalityJson.BirthDate;
                    dbSql.Add(personality);
                    dbSql.SaveChanges();
                    Guid newPersonalityGuid = personality.Guid;
                    PersonalityVersion personalityVersion = new();
                    personalityVersion.Name = personalityJson.Name;
                    personalityVersion.Surname = personalityJson.Surname;
                    personalityVersion.Patronymic = personalityJson.Patronymic;
                    personalityVersion.JobTitle = dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle);
                    personalityVersion.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                    personalityVersion.HireDate = personalityJson.HireDate;
                    personalityVersion.DismissalsDate = personalityJson.DismissalsDate;
                    personalityVersion.Schedule = dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule);
                    personalityVersion.Entity = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.Entity);
                    personalityVersion.EntityCost = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost);
                    DateTime now = DateTime.Now;
                    personalityVersion.VersionStartDate = now;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == newPersonalityGuid);
                    personalityVersion.Actual = personalityJson.Actual;
                    dbSql.Add(personalityVersion);
                    dbSql.SaveChanges();
                }
                else
                {
                    DateTime now = DateTime.Now;
                    dbSql.PersonalityVersions.FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null).VersionEndDate = now;
                    Models.MSSQL.Personality.Personality personality = new Personality();
                    Guid newPersonalityGuid = personalityJson.Guid;
                    PersonalityVersion personalityVersion = new();
                    personalityVersion.Name = personalityJson.Name;
                    personalityVersion.Surname = personalityJson.Surname;
                    personalityVersion.Patronymic = personalityJson.Patronymic;
                    personalityVersion.JobTitle = dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle);
                    personalityVersion.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                    personalityVersion.HireDate = personalityJson.HireDate;
                    personalityVersion.DismissalsDate = personalityJson.DismissalsDate;
                    personalityVersion.Schedule = dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule);
                    personalityVersion.Entity = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.Entity);
                    personalityVersion.EntityCost = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost);
                    personalityVersion.VersionStartDate = now;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                    personalityVersion.Actual = personalityJson.Actual;
                    dbSql.Add(personalityVersion);
                    dbSql.SaveChanges();
                }
                    


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
                PersonalityVersion personalityVersion = dbSql.PersonalityVersions.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                personalityVersion.Name = personalityJson.Name;
                personalityVersion.Surname = personalityJson.Surname;
                personalityVersion.Patronymic = personalityJson.Patronymic;
                personalityVersion.JobTitle = dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle);
                personalityVersion.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                personalityVersion.HireDate = personalityJson.HireDate;
                personalityVersion.DismissalsDate = personalityJson.DismissalsDate;
                personalityVersion.Schedule = dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule);
                personalityVersion.Entity = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.Entity);
                personalityVersion.EntityCost = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost);
                personalityVersion.VersionStartDate = personalityJson.VersionStartDate;
                personalityVersion.VersionEndDate = personalityJson.VersionEndDate;
                personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                personalityVersion.Actual = personalityJson.Actual;
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

        public IActionResult PersonalityVersions(string typeGuid, string newPerson)
        {
            PersonalityVersionModel personalityVersionModel = new();
            personalityVersionModel.NewPerson = newPerson;
            personalityVersionModel.Personality = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            personalityVersionModel.PersonalitiesVersions = dbSql.PersonalityVersions.Include(c => c.Personalities)
                                                                                     .Include(c => c.Location)
                                                                                     .Include(c => c.JobTitle)
                                                                                     .Include(c => c.Entity)
                                                                                     .Include(c => c.EntityCost)
                                                                                     .Include(c => c.Schedule)
                                                                                     .Where(c => c.Personalities.Guid == personalityVersionModel.Personality.Guid)
                                                                                     .ToList();
            return PartialView(personalityVersionModel);
        }

    }
}
