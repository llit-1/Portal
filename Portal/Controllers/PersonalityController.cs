using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
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
                    if (personalityVersion != null)
                    {
                        List<int> StatusError = new();
                        PersonalityModel model = new PersonalityModel();

                        var count = 0;

                        List<Guid> temp = new();

                        List<PersonalityVersion> checkVersionsForError = dbSql.PersonalityVersions.Include(c => c.Personalities)
                                                                                                  .Where(c => c.Personalities.Guid == person.Guid)
                                                                                                  .OrderBy(c => c.VersionStartDate)
                                                                                                  .ToList();

                        for (var i = 0; i < (checkVersionsForError.Count); i++)
                        {
                            if (i != (checkVersionsForError.Count - 1))
                            {

                                if (checkVersionsForError[i].VersionEndDate != null)
                                {
                                    if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionEndDate.Value.AddDays(1) != checkVersionsForError[i + 1].VersionStartDate)
                                    {
                                        temp.Add(checkVersionsForError[i].Guid);
                                    }
                                    else if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionEndDate.Value >= checkVersionsForError[i + 1].VersionStartDate.Value)
                                    {
                                        temp.Add(checkVersionsForError[i].Guid);
                                    }
                                }
                            }
                            if (checkVersionsForError[i].Actual == 1 && checkVersionsForError[i].VersionEndDate == null)
                            {
                                count++;
                            }
                        }
                        if (temp.Count >= 1 || count > 1)
                        {
                            StatusError.Add(1);
                        }
                        else
                        {
                            StatusError.Add(0);
                        }


                        model.StatusError = StatusError;
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
                                                                                     .FirstOrDefault(c => c.Personalities.Guid == person.Guid && c.Actual == 1 && c.VersionEndDate == null);
                    if (personalityVersion != null)
                    {
                        List<int> StatusError = new();
                        PersonalityModel model = new PersonalityModel();

                        var count = 0;

                        List<Guid> temp = new();

                        List<PersonalityVersion> checkVersionsForError = dbSql.PersonalityVersions.Include(c => c.Personalities)
                                                                                                  .Where(c => c.Personalities.Guid == person.Guid)
                                                                                                  .OrderBy(c => c.VersionStartDate)
                                                                                                  .ToList();

                        for (var i = 0; i < (checkVersionsForError.Count); i++)
                        {
                            if (i != (checkVersionsForError.Count - 1))
                            {

                                if (checkVersionsForError[i].VersionEndDate != null)
                                {
                                    if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionEndDate.Value.AddDays(1) != checkVersionsForError[i + 1].VersionStartDate)
                                    {
                                        temp.Add(checkVersionsForError[i].Guid);
                                    }
                                    else if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionEndDate.Value >= checkVersionsForError[i + 1].VersionStartDate.Value)
                                    {
                                        temp.Add(checkVersionsForError[i].Guid);
                                    }
                                }
                            }
                            if (checkVersionsForError[i].Actual == 1 && checkVersionsForError[i].VersionEndDate == null)
                            {
                                count++;
                            }
                        }
                        if (temp.Count >= 1 || count > 1)
                        {
                            StatusError.Add(1);
                        }
                        else
                        {
                            StatusError.Add(0);
                        }

                        model.StatusError = StatusError;
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
                                                                                  .FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(typeGuid));
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
                    personalityVersion.VersionStartDate = personalityJson.HireDate;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == newPersonalityGuid);
                    personalityVersion.Actual = personalityJson.Actual;
                    dbSql.Add(personalityVersion);
                    dbSql.SaveChanges();
                }
                else
                {
                    DateTime now = DateTime.Now;
                    // �������� �� ������ �������� ����� ���������� ������, ��� ������� ���������� 
                    if(personalityJson?.VersionEndDate == null)
                    {
                        if(dbSql.PersonalityVersions.FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null) != null)
                        {
                            dbSql.PersonalityVersions.FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null).VersionEndDate = now.AddDays(-1);
                        }
                    }

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
                    personalityVersion.VersionStartDate = personalityJson.VersionStartDate;
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
                PersonalityVersion personalityVersion = dbSql.PersonalityVersions.FirstOrDefault(c => c.Guid == personalityJson.Guid);
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
                personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                personalityVersion.Actual = personalityJson.Actual;

                List<PersonalityVersion> avalible = dbSql.PersonalityVersions.Where(c => c.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null)
                                                                             .ToList();

                List<PersonalityVersion> allPersonalityVersions = dbSql.PersonalityVersions.Where(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null)
                                                                                           .ToList();

                if (allPersonalityVersions.Count <= 1 && personalityJson.VersionEndDate != null && allPersonalityVersions[0].Guid == personalityJson.Guid)
                {
                    result.Ok = false;
                    result.ErrorMessage = "Изменение невозможно. У пользователя должна быть активная версия!";
                    return new ObjectResult(result);
                }
                else
                {
                    personalityVersion.VersionStartDate = personalityJson.VersionStartDate;
                    personalityVersion.VersionEndDate = personalityJson.VersionEndDate;
                    dbSql.SaveChanges();
                    return new OkObjectResult(result);
                }
                
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

            List<PersonalityVersion> checkVersionsForError = dbSql.PersonalityVersions.Include(c => c.Personalities)
                                                                                      .Include(c => c.Location)
                                                                                      .Include(c => c.JobTitle)
                                                                                      .Include(c => c.Entity)
                                                                                      .Include(c => c.EntityCost)
                                                                                      .Include(c => c.Schedule)
                                                                                      .Where(c => c.Personalities.Guid == Guid.Parse(typeGuid))
                                                                                      .OrderBy(c => c.VersionStartDate)
                                                                                      .ToList();


            personalityVersionModel.NewPerson = newPerson;
            personalityVersionModel.Personality = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            personalityVersionModel.PersonalitiesVersions = checkVersionsForError;


            List<Guid> ErrorsInDAtes = new();
            var count = 0;
            List<Guid> temp = new();
            
            for (var i = 0; i < checkVersionsForError.Count; i++)
            {      
                if(checkVersionsForError[i].VersionEndDate != null)
                {
                    if(i < (checkVersionsForError.Count - 1))
                    {
                        if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionEndDate.Value.AddDays(1) != checkVersionsForError[i + 1].VersionStartDate)
                        {
                            ErrorsInDAtes.Add(checkVersionsForError[i].Guid);
                        }
                        else if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionEndDate.Value >= checkVersionsForError[i + 1].VersionStartDate.Value)
                        {
                            ErrorsInDAtes.Add(checkVersionsForError[i].Guid);
                        }
                    }
                    
                }
                else
                {
                    count++;
                    if(count > 1)
                    {
                        ErrorsInDAtes.Add(checkVersionsForError[i].Guid);
                    }
                    temp.Add(checkVersionsForError[i].Guid);
                }
            }
            if(temp.Count > 1)
            {
                temp.RemoveAt(temp.Count - 1);
                ErrorsInDAtes.AddRange(temp);
            }

            personalityVersionModel.Errors = ErrorsInDAtes;

            return PartialView(personalityVersionModel);
        }

    }
}
