using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Portal.Models;
using Portal.Models.JsonModels;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using RKNet_Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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

        /* Проверка дат на пересечение и прочие ошибки */
        public List<int> checkError(PersonalityVersion personalityVersion, Personality person)
        {
            List<int> StatusError = new();

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
                        else if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionStartDate.Value > checkVersionsForError[i].VersionEndDate.Value)
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

            return StatusError;
        }

        /* Отображение таблицы с сотрудниками, принимает актуальность, номер страницы и\или отдельно взятого пользователя */
        public IActionResult PersonalityTable(string showUnActual, string page, string searchItem)
        {
            if(int.Parse(page) == 1)
            {
                page = "1";
            }

            // Выбор нужных записей для страницы
            List<PersonalityModel> models = new List<PersonalityModel> ();
            List<Personality> personalities;
            
            /* Считаем кол-во уникальных сотрудников */
            int countPage = dbSql.Personalities.Count();

            /* Расчет необходимых данных для страницы */
            if (searchItem == null)
            { 
                personalities = dbSql.Personalities.Skip((int.Parse(page) - 1) * 30).Take(30).OrderBy(x => x.Name).ToList();
            }
            else
            {
                personalities = dbSql.Personalities.AsEnumerable()
                                                   .Where(x => string.Join("", x.Name.Split(" ", StringSplitOptions.RemoveEmptyEntries).Take(2))
                                                   .ToLower().Contains(searchItem.ToLower()))
                                                   .ToList();

                if (personalities.Count != 0)
                {
                    countPage = personalities.Count();
                    var k = int.Parse(page) - 1;
                    if(countPage < 30)
                    {
                        personalities = personalities.Skip((k - 1) * 30).Take(30).ToList();
                    } else
                    {
                        personalities = personalities.Skip((int.Parse(page) - 1) * 30).Take(30).ToList();
                    }
                    
                }
                else
                {
                    countPage = 0;
                }
                
            }
            
            // всего страниц
            int maxPage = (int)Math.Ceiling((double)countPage / 30);
            // сколько страниц осталось относительно текущей
            int pageLeft = maxPage - int.Parse(page);

            foreach (var person in personalities)
            {
                PersonalityVersion personalityVersion;

                // Выбор записей для активных \ не активных пользователей
                personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                              .Include(c => c.Location)
                                                              .Include(c => c.Personalities)
                                                              .Include(c => c.Schedule)
                                                              .Include(c => c.Entity)
                                                              .FirstOrDefault(c => c.Personalities.Guid == person.Guid && c.VersionEndDate == null);             

                PersonalityModel model = new PersonalityModel();
                model.maxPage = maxPage;
                model.currentPage = int.Parse(page);
                model.StatusError = checkError(personalityVersion, person);
                model.Personalities = person;
                model.PersonalitiesVersions = personalityVersion;
                model.Entity = dbSql.Entity.ToList();
                models.Add(model);
            }
            return PartialView(models.ToList());
        }

        /* Редактирование карточки сотрудника */
        public IActionResult PersonalityEdit(string typeGuid, string newPerson)
        {
            PersonalityEditModel model = new();
            PersonalityVersion personalityVersion;

            if (newPerson == "2" && typeGuid != "0")
            {
                personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
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
                personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                              .Include(c => c.Location)
                                                              .Include(c => c.Schedule)
                                                              .Include(c => c.Personalities)
                                                              .Include(c => c.Entity)
                                                              .FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(typeGuid) && c.Actual == 1);
                if(personalityVersion != null)
                {
                    model.PersonalitiesVersions = personalityVersion;
                } else
                {
                    personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                                  .Include(c => c.Location)
                                                                  .Include(c => c.Schedule)
                                                                  .Include(c => c.Personalities)
                                                                  .Include(c => c.Entity)
                                                                  .FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(typeGuid));
                    model.PersonalitiesVersions = personalityVersion;
                }
                
                model.Personality = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(typeGuid));
            }

            
            model.NewPerson = newPerson;
            model.Schedules = dbSql.Schedules.ToList();
            model.Locations = dbSql.Locations.ToList();
            model.JobTitles = dbSql.JobTitles.ToList();
            model.Entity = dbSql.Entity.ToList();
            //model.CurrentPage = getUserPage(model.PersonalitiesVersions.Personalities.Guid.ToString());
            return PartialView(model);
        }

        /* Получение списка зависимостей Должностей\Тайм-слотов */
        [HttpPost]
        public IActionResult GetScheduleList(string jobTitleName)
        {
            var result = dbSql.Schedules.Where(x => x.Name == jobTitleName).OrderBy(x => x.BeginTime).ToList();
            return Json(result);
        }

        /* Добавление сотрудника */
        public IActionResult PersonalityAdd(string json)
        {
            var result = new Result<string>();
            try
            {
                PersonalityJson personalityJson = JsonConvert.DeserializeObject<PersonalityJson>(json);
                if (personalityJson.NewPerson == "1")
                {
                    /* Добавление нового сотрудника и его первой версии */
                    Personality personality = new Personality();
                    personality.Name = personalityJson.Surname + " " + personalityJson.Name + " " + personalityJson.Patronymic;
                    personality.BirthDate = personalityJson.BirthDate;
                    personality.Phone = personalityJson.Tel;

                    if(personality.Phone.Length != 10)
                    {
                        return BadRequest("Номер телефона должен содержать 10 символов и иметь вид 9ХХХХХХХХХ");
                    }

                    // 911 174 40 10

                    if(dbSql.Personalities.FirstOrDefault(x => x.Name == personality.Name) != null && dbSql.Personalities.FirstOrDefault(x => x.BirthDate == personality.BirthDate) != null)
                    {
                        return BadRequest("Пользователь уже существует");
                    }

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
                    personalityVersion.EntityCostGuid = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost).Guid;
                    DateTime now = DateTime.Now;
                    personalityVersion.VersionStartDate = personalityJson.HireDate;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == newPersonalityGuid);
                    personalityVersion.Actual = personalityJson.Actual;
                    //personalityVersion.Tel = personalityJson.Tel;
                    dbSql.Add(personalityVersion);
                    dbSql.SaveChanges();
                }
                else
                {
                    /* Добавление новой версии уже существующего пользователя */
                    DateTime now = DateTime.Now;
                    if(personalityJson?.VersionEndDate == null)
                    {
                        if(dbSql.PersonalityVersions.FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null) != null)
                        {
                            dbSql.PersonalityVersions.FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null).VersionEndDate = personalityJson.VersionStartDate.Value.AddDays(-1);
                            dbSql.PersonalityVersions.FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null).Actual = 0;
                        }
                    }

                    Personality personality = new Personality();
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
                    personalityVersion.EntityCostGuid = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost).Guid;
                    personalityVersion.VersionStartDate = personalityJson.VersionStartDate;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                    personalityVersion.Personalities.Phone = personalityJson.Tel;
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
            var result = new Result<string>();
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
                personalityVersion.EntityCostGuid = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost).Guid;
                personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                personalityVersion.Actual = personalityJson.Actual;
                personalityVersion.Personalities.Phone = personalityJson.Tel;
                personalityVersion.Personalities.BirthDate = personalityJson.BirthDate;

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

        /* Вывод версий сотрудника и проверка дат на пересечение */
        public IActionResult PersonalityVersions(string typeGuid, string newPerson)
        {
            PersonalityVersionModel personalityVersionModel = new();

            List<PersonalityVersion> checkVersionsForError = dbSql.PersonalityVersions.Include(c => c.Personalities)
                                                                                      .Include(c => c.Location)
                                                                                      .Include(c => c.JobTitle)
                                                                                      .Include(c => c.Entity)
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
                        else if (checkVersionsForError.Count > 1 && checkVersionsForError[i].VersionStartDate.Value > checkVersionsForError[i].VersionEndDate.Value)
                        {
                            temp.Add(checkVersionsForError[i].Guid);
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
            personalityVersionModel.Entity = dbSql.Entity.ToList();
            personalityVersionModel.CurrentPage = getUserPage(typeGuid);

            return PartialView(personalityVersionModel);
        }

        public int getUserPage(string GuidUser)
        {
            List<Personality> personality = dbSql.Personalities.ToList();
            var countUser = personality.Count();
            int numberOfUser = 1;
            int result;
            for(var i = 0; i < countUser; i++)
            {
                if (personality[i].Guid != Guid.Parse(GuidUser))
                {
                    continue;
                }
                numberOfUser = i + 1;
            }

            result = (int)Math.Ceiling((double)numberOfUser / 30);


            return result;
        }
    }
}
