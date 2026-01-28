using DocumentFormat.OpenXml.Office2019.Excel.RichData2;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Portal.Models;
using Portal.Models.JsonModels;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Factory;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using RKNet_Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Portal.Controllers
{
    public class PersonalityController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        private readonly IMemoryCache _cache;
        public PersonalityController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IMemoryCache cache)
{
            db = context;
            dbSql = dbSqlContext;
            _cache = cache;
        }
        [Authorize(Roles = "HR, employee_control_ukvh, settings")]
        public IActionResult Personality()
        {
            return PartialView();
        }
        /* Проверка дат на пересечение и прочие ошибки */
        public bool HasErrors(List<PersonalityVersion> versions)
        {
            int count = 0;
            List<Guid> temp = new();
            for (int i = 0; i < versions.Count; i++)
            {
                if (i < versions.Count - 1)
                {
                    var current = versions[i];
                    var next = versions[i + 1];
                    if (current.VersionEndDate != null)
                    {
                        if (current.VersionEndDate.Value.AddDays(1) != next.VersionStartDate)
                        {
                            temp.Add(current.Guid);
                        }
                        else if (current.VersionEndDate.Value >= next.VersionStartDate)
                        {
                            temp.Add(current.Guid);
                        }
                        else if (current.VersionStartDate > current.VersionEndDate)
                        {
                            temp.Add(current.Guid);
                        }
                    }
                }
                if (versions[i].Actual == 1 && versions[i].VersionEndDate == null)
                {
                    count++;
                }
            }
            return temp.Count > 0 || count > 1;
        }
        [Authorize(Roles = "HR, employee_control_ukvh, settings")]
        public IActionResult PersonalityTable(int showUnActual, int checkErrors)
        {
            ViewBag.ShowUnActual = showUnActual;
            ViewBag.CheckErrors = checkErrors;
            return PartialView();
        }
        [Authorize(Roles = "HR, employee_control_ukvh, settings")]
        [HttpPost]
        public IActionResult PersonalityTableData(int showUnActual, int checkErrors)
        {
            try
            {
                dbSql.Database.SetCommandTimeout(120);
                var draw = Convert.ToInt32(Request.Form["draw"].FirstOrDefault() ?? "1");
                var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
                var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "18");
                var searchValue = (Request.Form["search[value]"].FirstOrDefault() ?? string.Empty).Trim();
                var orderColumnIndex = Convert.ToInt32(Request.Form["order[0][column]"].FirstOrDefault() ?? "0");
                var orderDir = (Request.Form["order[0][dir]"].FirstOrDefault() ?? "asc").ToLowerInvariant();
                var minDate = new DateTime(1753, 1, 1);
                // -------------------------
                // errors cache (как у тебя)
                // -------------------------
                HashSet<Guid> errorGuidsSet = null;
                if (checkErrors == 1)
                {
                    const string errorsCacheKey = "personality:errors";
                    if (!_cache.TryGetValue(errorsCacheKey, out errorGuidsSet))
                    {
                        var versionsMin = dbSql.PersonalityVersions
                            .AsNoTracking()
                            .Select(x => new
                            {
                                x.Guid,
                                PersonalityGuid = x.Personalities.Guid,
                                x.Actual,
                                x.VersionStartDate,
                                x.VersionEndDate
                            })
                            .ToList();
                        var versionsByPersonality = versionsMin
                            .GroupBy(x => x.PersonalityGuid)
                            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.VersionStartDate).ToList());
                        var errorGuids = new List<Guid>(versionsByPersonality.Count);
                        foreach (var kv in versionsByPersonality)
                        {
                            var versions = kv.Value
                                .Select(x => new PersonalityVersion
                                {
                                    Guid = x.Guid,
                                    VersionStartDate = x.VersionStartDate,
                                    VersionEndDate = x.VersionEndDate,
                                    Actual = x.Actual
                                })
                                .ToList();
                            if (HasErrors(versions))
                                errorGuids.Add(kv.Key);
                        }
                        errorGuidsSet = new HashSet<Guid>(errorGuids);
                        _cache.Set(errorsCacheKey, errorGuidsSet, TimeSpan.FromMinutes(5));
                    }
                }
                // ------------------------------------------------
                // selectedVersionIdsQuery (как у тебя, но аккуратно)
                // ------------------------------------------------
                IQueryable<Guid> selectedVersionIdsQuery;
                if (showUnActual == 1)
                {
                    selectedVersionIdsQuery = dbSql.PersonalityVersions
                        .AsNoTracking()
                        .Where(v => v.Actual == 1)
                        .Select(v => v.Guid);
                }
                else
                {
                    var actualPersonalityIds = dbSql.PersonalityVersions
                        .AsNoTracking()
                        .Where(v => v.Actual == 1)
                        .Select(v => v.Personalities.Guid);
                    var latestNonActualPerPerson = dbSql.PersonalityVersions
                        .AsNoTracking()
                        .Where(v => v.Actual != 1 && !actualPersonalityIds.Contains(v.Personalities.Guid))
                        .GroupBy(v => v.Personalities.Guid)
                        .Select(g => new
                        {
                            PersonalityGuid = g.Key,
                            MaxDate = g.Max(v => (v.VersionStartDate ?? minDate))
                        });
                    var latestNonActualIds =
                        from v in dbSql.PersonalityVersions.AsNoTracking()
                        join m in latestNonActualPerPerson
                            on new
                            {
                                PersonalityGuid = v.Personalities.Guid,
                                MaxDate = (v.VersionStartDate ?? minDate)
                            }
                            equals new
                            {
                                PersonalityGuid = m.PersonalityGuid,
                                MaxDate = m.MaxDate
                            }
                        where v.Actual != 1 && !actualPersonalityIds.Contains(v.Personalities.Guid)
                        select v.Guid;
                    var actualVersionIds = dbSql.PersonalityVersions
                        .AsNoTracking()
                        .Where(v => v.Actual == 1)
                        .Select(v => v.Guid);
                    selectedVersionIdsQuery = actualVersionIds.Union(latestNonActualIds);
                }
                // ------------------------------------------------------------
                // baseQuery: добавили Guid-поля для дешёвой сортировки (ВАЖНО!)
                // ------------------------------------------------------------
                var baseQuery =
                    from v in dbSql.PersonalityVersions.AsNoTracking()
                    join cost in dbSql.Entity.AsNoTracking()
                        on v.EntityCostGuid equals cost.Guid into costJoin
                    from cost in costJoin.DefaultIfEmpty()
                    where selectedVersionIdsQuery.Contains(v.Guid)
                    select new
                    {
                        v.Guid,
                        PersonGuid = v.Personalities.Guid,
                        v.Surname,
                        v.Name,
                        v.Patronymic,
                        JobTitleGuid = v.JobTitle != null ? (Guid?)v.JobTitle.Guid : null,
                        JobTitleName = v.JobTitle != null ? v.JobTitle.Name : "Парт-таймер",
                        LocationGuid = v.Location.Guid,
                        LocationName = v.Location.Name,
                        v.HireDate,
                        v.DismissalsDate,
                        EntityGuid = v.Entity != null ? (Guid?)v.Entity.Guid : null,
                        EntityName = v.Entity != null ? v.Entity.Name : "",
                        v.EntityCostGuid,
                        EntityCostName = cost != null ? cost.Name : "",
                        v.Actual
                    };
                // фильтр по роли
                if (User.IsInRole("employee_control_ukvh"))
                {
                    var roleGuid = Guid.Parse("27DF2DD0-2EBE-4CDE-A46C-08DBF1826A1F");
                    baseQuery = baseQuery.Where(x => x.EntityCostGuid == roleGuid);
                }
                // фильтр "только с ошибками"
                if (checkErrors == 1 && errorGuidsSet != null)
                {
                    baseQuery = baseQuery.Where(x => errorGuidsSet.Contains(x.PersonGuid));
                }
                var recordsTotal = baseQuery.Count();
                // поиск
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    baseQuery = baseQuery.Where(x =>
                        (x.Surname ?? "").Contains(searchValue) ||
                        (x.Name ?? "").Contains(searchValue) ||
                        (x.Patronymic ?? "").Contains(searchValue) ||
                        (x.JobTitleName ?? "").Contains(searchValue) ||
                        (x.LocationName ?? "").Contains(searchValue) ||
                        (x.EntityName ?? "").Contains(searchValue) ||
                        (x.EntityCostName ?? "").Contains(searchValue)
                    );
                }
                var recordsFiltered = baseQuery.Count();
                // ---------------------------------------------------------
                // СОРТИРОВКА: для 3/4/7/8 сортируем по GUID (быстрее),
                // и везде добавляем ThenBy(PersonGuid) как tie-breaker
                // ---------------------------------------------------------
                bool desc = orderDir == "desc";
                baseQuery = orderColumnIndex switch
                {
                    0 => desc
                        ? baseQuery.OrderByDescending(x => x.Surname).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.Surname).ThenBy(x => x.PersonGuid),
                    1 => desc
                        ? baseQuery.OrderByDescending(x => x.Name).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.Name).ThenBy(x => x.PersonGuid),
                    2 => desc
                        ? baseQuery.OrderByDescending(x => x.Patronymic).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.Patronymic).ThenBy(x => x.PersonGuid),
                    // ДЁШЕВО: сортировка по ключам вместо строк
                    3 => desc
                        ? baseQuery.OrderByDescending(x => x.JobTitleGuid).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.JobTitleGuid).ThenBy(x => x.PersonGuid),
                    4 => desc
                        ? baseQuery.OrderByDescending(x => x.LocationGuid).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.LocationGuid).ThenBy(x => x.PersonGuid),
                    5 => desc
                        ? baseQuery.OrderByDescending(x => x.HireDate).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.HireDate).ThenBy(x => x.PersonGuid),
                    6 => desc
                        ? baseQuery.OrderByDescending(x => x.DismissalsDate).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.DismissalsDate).ThenBy(x => x.PersonGuid),
                    7 => desc
                        ? baseQuery.OrderByDescending(x => x.EntityGuid).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.EntityGuid).ThenBy(x => x.PersonGuid),
                    8 => desc
                        ? baseQuery.OrderByDescending(x => x.EntityCostGuid).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.EntityCostGuid).ThenBy(x => x.PersonGuid),
                    _ => desc
                        ? baseQuery.OrderByDescending(x => x.Surname).ThenBy(x => x.PersonGuid)
                        : baseQuery.OrderBy(x => x.Surname).ThenBy(x => x.PersonGuid)
                };
                var page = baseQuery.Skip(start).Take(length).ToList();
                var data = page.Select(x => new
                {
                    personGuid = x.PersonGuid,
                    surname = x.Surname,
                    name = x.Name,
                    patronymic = x.Patronymic,
                    jobTitle = x.JobTitleName,
                    location = x.LocationName,
                    hireDate = x.HireDate.ToString("dd.MM.yyyy"),
                    dismissalsDate = x.DismissalsDate?.ToString("dd.MM.yyyy") ?? "",
                    entity = x.EntityName,
                    entityCost = x.EntityCostName,
                    status = x.Actual == 0 ? "Уволен" : x.Actual == 1 ? "Активен" : "Отстранен",
                    actual = x.Actual,
                    hasErrors = errorGuidsSet != null && errorGuidsSet.Contains(x.PersonGuid)
                });
                return Json(new
                {
                    draw,
                    recordsTotal,
                    recordsFiltered,
                    data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [Authorize(Roles = "HR, employee_control_ukvh, settings")]
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
                if(personalityVersion.Personalities.LMKID != null)
                {
                    model.PersonalityLMK = dbSql.PersonalityLMK.FirstOrDefault(x => x.Id == personalityVersion.Personalities.LMKID);
                }
                ViewBag.TTCount = dbSql.BindingPersonalityToLocation.Where(x => x.Personality == model.PersonalitiesVersions.Personalities.Guid)?.Count();
            }
            else if(typeGuid != "0")
            {
                personalityVersion = dbSql.PersonalityVersions.Include(c => c.JobTitle)
                                                              .Include(c => c.Location)
                                                              .Include(c => c.Schedule)
                                                              .Include(c => c.Personalities)
                                                              .Include(c => c.Entity)
                                                              .FirstOrDefault(c => c.Personalities.Guid == Guid.Parse(typeGuid) && c.Actual == 1);
                if (personalityVersion != null)
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
                if (personalityVersion.Personalities.LMKID != null)
                {
                    model.PersonalityLMK = dbSql.PersonalityLMK.FirstOrDefault(x => x.Id == personalityVersion.Personalities.LMKID);
                }
                ViewBag.TTCount = dbSql.BindingPersonalityToLocation.Where(x => x.Personality == model.PersonalitiesVersions.Personalities.Guid)?.Count();
            }
            model.NewPerson = newPerson;
            model.Schedules = dbSql.Schedules.ToList();
            model.Locations = dbSql.Locations.ToList();
            model.JobTitles = dbSql.JobTitles.ToList();
            model.DocumentTypes = dbSql.PersonalityDocumentTypes.ToList();
            model.PersonalityCitizenship = dbSql.PersonalityCitizenship.ToList();
            model.Entity = dbSql.Entity.ToList();
            return PartialView(model);
        }
        public IActionResult GetFile(string guid)
        {
            var entity = dbSql.Personalities.FirstOrDefault(x => x.Guid == Guid.Parse(guid));
            var lmk = dbSql.PersonalityLMK.FirstOrDefault(x => x.Id == entity.LMKID);
            if (lmk == null || lmk.FileData == null || lmk.FileData.Length == 0)
                return NotFound();
            var fileBytes = lmk.FileData;
            // можно определить MIME, либо ставить универсальный
            var mimeType = "application/octet-stream";
            return File(fileBytes, mimeType);
        }
        /* Получение списка зависимостей Должностей\Тайм-слотов */
        [HttpPost]
        public IActionResult GetScheduleList(string jobTitleGuid)
        {
            var result = dbSql.Schedules.Where(x => x.JobTitleGuid == Guid.Parse(jobTitleGuid)).OrderBy(x => x.BeginTime).ToList();
            return Json(result);
        }
        /* Добавление сотрудника */
        [Authorize(Roles = "HR, employee_control_ukvh, settings")]
        public async Task<IActionResult> PersonalityAdd()
        {
            var result = new Result<string>();
            try
            {
                // Получаем JSON из FormData
                var json = Request.Form["json"].FirstOrDefault();
                if (string.IsNullOrEmpty(json))
                {
                    result.Ok = false;
                    result.ErrorMessage = "Отсутствуют данные пользователя";
                    return BadRequest("Отсутствуют данные пользователя");
                }
                PersonalityJson personalityJson = JsonConvert.DeserializeObject<PersonalityJson>(json);
                if (personalityJson.NewPerson == "1")
                {
                    Personality personality = new Personality();
                    PersonalityLMK personalityLMK = new PersonalityLMK();
                    /* Добавление нового сотрудника и его первой версии */
                    personality.Name = personalityJson.Surname + " " + personalityJson.Name + " " + personalityJson.Patronymic;
                    personality.BirthDate = personalityJson.BirthDate;
                    personality.Phone = personalityJson.Tel;
                    personalityLMK.DocumentTypeId = personalityJson.LMK.DocumentTypeId;
                    personalityLMK.VacGepatit = personalityJson.LMK.VacGepatit;
                    personalityLMK.VacReject = personalityJson.LMK.VacReject;
                    personalityLMK.FLGDate = personalityJson.LMK.FLGDate;
                    personalityLMK.ValidationDate = personalityJson.LMK.ValidationDate;
                    personalityLMK.MedComissionDate = personalityJson.LMK.MedComissionDate;
                    personalityLMK.VacZonne = personalityJson.LMK.VacZonne;
                    // Обработка файла, если он есть
                    var file = Request.Form.Files.FirstOrDefault();
                    if (file != null && file.Length > 0)
                    {
                        // Сохраняем только данные файла
                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            personalityLMK.FileData = memoryStream.ToArray();
                        }
                    }
                    dbSql.PersonalityLMK.Add(personalityLMK);
                    dbSql.SaveChanges();
                    personality.LMKID = personalityLMK.Id;
                    if (personality.Phone.Length != 10)
                    {
                        return BadRequest("Номер телефона должен содержать 10 символов и иметь вид 9ХХХХХХХХХ");
                    }
                    if(dbSql.Personalities.FirstOrDefault(x => x.Name.ToLower().Trim() == personality.Name.ToLower().Trim()) != null && dbSql.Personalities.FirstOrDefault(x => x.BirthDate == personality.BirthDate) != null)
                    {
                        return BadRequest("Пользователь с таким ФИО и датой рождения уже существует");
                    }
                    if(personalityJson.SNILS != "" && dbSql.Personalities.FirstOrDefault(x => x.SNILS == personalityJson.SNILS) != null)
                    {
                        var person = dbSql.Personalities.FirstOrDefault(x => x.SNILS == personalityJson.SNILS);
                        var personVersion = dbSql.PersonalityVersions.FirstOrDefault(x => x.Personalities == person);
                        return BadRequest("Указанный снилс уже закреплен за " + personVersion.Surname + " " + personVersion.Name + " " + personVersion.Patronymic);
                    }
                    if (personalityJson.INN != "" && dbSql.Personalities.FirstOrDefault(x => x.INN == personalityJson.INN) != null)
                    {
                        var person = dbSql.Personalities.FirstOrDefault(x => x.INN == personalityJson.INN);
                        var personVersion = dbSql.PersonalityVersions.FirstOrDefault(x => x.Personalities == person);
                        return BadRequest("Указанный ИНН уже закреплен за " + personVersion.Surname + " " + personVersion.Name + " " + personVersion.Patronymic);
                    }
                    dbSql.Add(personality);
                    dbSql.SaveChanges();
                    Guid newPersonalityGuid = personality.Guid;
                    PersonalityVersion personalityVersion = new();
                    personalityVersion.Name = personalityJson.Name;
                    personalityVersion.Surname = personalityJson.Surname;
                    personalityVersion.Patronymic = personalityJson.Patronymic;
                    personalityVersion.JobTitle = personalityJson.JobTitle.HasValue
                                                                                    ? dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle)
                                                                                    : null;
                    personalityVersion.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                    personalityVersion.HireDate = personalityJson.HireDate;
                    personalityVersion.DismissalsDate = personalityJson.DismissalsDate;
                    personalityVersion.Schedule = personalityJson.Schedule.HasValue
                                                                                ? dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule)
                                                                                : null;
                    personalityVersion.Entity = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.Entity);
                    personalityVersion.EntityCostGuid = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost).Guid;
                    DateTime now = DateTime.Now;
                    personalityVersion.VersionStartDate = personalityJson.HireDate;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == newPersonalityGuid);
                    personalityVersion.Actual = personalityJson.Actual;
                    personalityVersion.ModifiedBy = User.Identity.Name;
                    personalityVersion.ModifiedDate = DateTime.Now;
                    personalityVersion.Personalities.INN = personalityJson.INN;
                    personalityVersion.Personalities.SNILS = personalityJson.SNILS;
                    personalityVersion.Personalities.PersonalityCitizenship = personalityJson.PersonalityCitizenship;
                    personalityVersion.PartTimer = personalityJson.PartTimer;
                    personalityVersion.EntityCostDMSGuid = personalityJson.EntityCostDMS;
                    dbSql.Add(personalityVersion);
                    dbSql.SaveChanges();
                }
                else
                {
                    PersonalityVersion personalityVersion = new();
                    Personality personality = new Personality();
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
                    PersonalityVersion person = dbSql.PersonalityVersions.Include(x => x.Personalities).FirstOrDefault(x => x.Personalities.Guid == Guid.Parse(personalityJson.personGUID));
                    PersonalityLMK personalityLMK = new();
                    if (person.Personalities.LMKID != null)
                    {
                        personalityLMK = dbSql.PersonalityLMK.FirstOrDefault(x => x.Id == person.Personalities.LMKID);
                    }
                    personalityLMK.DocumentTypeId = personalityJson.LMK.DocumentTypeId;
                    personalityLMK.VacGepatit = personalityJson.LMK.VacGepatit;
                    personalityLMK.VacReject = personalityJson.LMK.VacReject;
                    personalityLMK.FLGDate = personalityJson.LMK.FLGDate;
                    personalityLMK.ValidationDate = personalityJson.LMK.ValidationDate;
                    personalityLMK.MedComissionDate = personalityJson.LMK.MedComissionDate;
                    personalityLMK.VacZonne = personalityJson.LMK.VacZonne;
                    // Обработка файла, если он есть
                    var file = Request.Form.Files.FirstOrDefault();
                    if (file != null && file.Length > 0)
                    {
                        // Сохраняем только данные файла
                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            personalityLMK.FileData = memoryStream.ToArray();
                        }
                    }
                    if (person.Personalities.LMKID == null)
                    {
                        dbSql.PersonalityLMK.Add(personalityLMK);
                    }
                    dbSql.SaveChanges();
                    person.Personalities.LMKID = personalityLMK.Id;
                    Guid newPersonalityGuid = personalityJson.Guid;
                    personalityVersion.Name = personalityJson.Name;
                    personalityVersion.Surname = personalityJson.Surname;
                    personalityVersion.Patronymic = personalityJson.Patronymic;
                    personalityVersion.JobTitle = personalityJson.JobTitle.HasValue
                                                                                    ? dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle)
                                                                                    : null;
                    personalityVersion.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                    personalityVersion.HireDate = personalityJson.HireDate;
                    personalityVersion.DismissalsDate = personalityJson.DismissalsDate;
                    personalityVersion.Schedule = personalityJson.Schedule.HasValue
                                                                                ? dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule)
                                                                                : null;
                    personalityVersion.Entity = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.Entity);
                    personalityVersion.EntityCostGuid = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost).Guid;
                    personalityVersion.VersionStartDate = personalityJson.VersionStartDate;
                    personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                    personalityVersion.Personalities.Phone = personalityJson.Tel;
                    personalityVersion.Actual = personalityJson.Actual;
                    personalityVersion.ModifiedBy = User.Identity.Name;
                    personalityVersion.ModifiedDate = DateTime.Now;
                    personalityVersion.Personalities.INN = personalityJson.INN;
                    personalityVersion.Personalities.SNILS = personalityJson.SNILS;
                    personalityVersion.Personalities.PersonalityCitizenship = personalityJson.PersonalityCitizenship;
                    personalityVersion.PartTimer = personalityJson.PartTimer;
                    personalityVersion.EntityCostDMSGuid = personalityJson.EntityCostDMS;
                    dbSql.Add(personalityVersion);
                    dbSql.SaveChanges();
                }
                _cache.Remove("personality:selectedIds");
                _cache.Remove("personality:errors");
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
        }
        [Authorize(Roles = "HR, employee_control_ukvh, settings")]
        public IActionResult PersonalityPut()
        {
            var result = new Result<string>();
            try
            {
                // Получаем JSON из FormData
                var json = Request.Form["json"].FirstOrDefault();
                if (string.IsNullOrEmpty(json))
                {
                    result.Ok = false;
                    result.ErrorMessage = "Отсутствуют данные пользователя";
                    return BadRequest(result);
                }
                PersonalityJson personalityJson = JsonConvert.DeserializeObject<PersonalityJson>(json);
                PersonalityLMK personalityLMK = new PersonalityLMK();
                PersonalityVersion personalityVersion = dbSql.PersonalityVersions.Include(p => p.JobTitle)
                                                                                 .Include(p => p.Schedule)
                                                                                 .Include(p => p.Personalities)
                                                                                 .FirstOrDefault(c => c.Guid == personalityJson.Guid);
                if (personalityJson.LMK != null)
                {
                    if (personalityVersion.Personalities.LMKID != null)
                    {
                        personalityLMK = dbSql.PersonalityLMK.FirstOrDefault(x => x.Id == personalityVersion.Personalities.LMKID);
                        personalityLMK.DocumentTypeId = personalityJson.LMK.DocumentTypeId;
                        personalityLMK.VacGepatit = personalityJson.LMK.VacGepatit;
                        personalityLMK.VacReject = personalityJson.LMK.VacReject;
                        personalityLMK.FLGDate = personalityJson.LMK.FLGDate;
                        personalityLMK.ValidationDate = personalityJson.LMK.ValidationDate;
                        personalityLMK.MedComissionDate = personalityJson.LMK.MedComissionDate;
                        personalityLMK.VacZonne = personalityJson.LMK.VacZonne;
                        // Обработка файла, если он есть
                        var file = Request.Form.Files.FirstOrDefault();
                        if (file != null && file.Length > 0)
                        {
                            // Сохраняем только данные файла
                            using (var memoryStream = new MemoryStream())
                            {
                                file.CopyTo(memoryStream);
                                personalityLMK.FileData = memoryStream.ToArray();
                            }
                        }
                        dbSql.PersonalityLMK.Update(personalityLMK);
                        dbSql.SaveChanges();
                    }
                    else
                    {
                        personalityLMK.DocumentTypeId = personalityJson.LMK.DocumentTypeId;
                        personalityLMK.VacGepatit = personalityJson.LMK.VacGepatit;
                        personalityLMK.VacReject = personalityJson.LMK.VacReject;
                        personalityLMK.FLGDate = personalityJson.LMK.FLGDate;
                        personalityLMK.ValidationDate = personalityJson.LMK.ValidationDate;
                        personalityLMK.MedComissionDate = personalityJson.LMK.MedComissionDate;
                        personalityLMK.VacZonne = personalityJson.LMK.VacZonne;
                        // Обработка файла, если он есть
                        var file = Request.Form.Files.FirstOrDefault();
                        if (file != null && file.Length > 0)
                        {
                            // Сохраняем только данные файла
                            using (var memoryStream = new MemoryStream())
                            {
                                file.CopyTo(memoryStream);
                                personalityLMK.FileData = memoryStream.ToArray();
                            }
                        }
                        dbSql.PersonalityLMK.Add(personalityLMK);
                        dbSql.SaveChanges();
                        personalityVersion.Personalities.LMKID = personalityLMK.Id;
                    }
                }
                personalityVersion.Name = personalityJson.Name;
                personalityVersion.Surname = personalityJson.Surname;
                personalityVersion.Patronymic = personalityJson.Patronymic;
                personalityVersion.JobTitle = personalityVersion.JobTitle = personalityJson.JobTitle.HasValue
                                                                                    ? dbSql.JobTitles.FirstOrDefault(c => c.Guid == personalityJson.JobTitle)
                                                                                    : null;
                personalityVersion.Location = dbSql.Locations.FirstOrDefault(c => c.Guid == personalityJson.Location);
                personalityVersion.HireDate = personalityJson.HireDate;
                personalityVersion.DismissalsDate = personalityJson.DismissalsDate;
                personalityVersion.Schedule = personalityJson.Schedule.HasValue
                                                                                ? dbSql.Schedules.FirstOrDefault(c => c.Guid == personalityJson.Schedule)
                                                                                : null;
                personalityVersion.Entity = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.Entity);
                personalityVersion.EntityCostGuid = dbSql.Entity.FirstOrDefault(c => c.Guid == personalityJson.EntityCost).Guid;
                personalityVersion.Personalities = dbSql.Personalities.FirstOrDefault(c => c.Guid == Guid.Parse(personalityJson.personGUID));
                personalityVersion.Actual = personalityJson.Actual;
                personalityVersion.Personalities.Phone = personalityJson.Tel;
                personalityVersion.Personalities.BirthDate = personalityJson.BirthDate;
                personalityVersion.ModifiedBy = User.Identity.Name;
                personalityVersion.ModifiedDate = DateTime.Now;
                personalityVersion.Personalities.PersonalityCitizenship = personalityJson.PersonalityCitizenship;
                personalityVersion.Personalities.INN = personalityJson.INN;
                personalityVersion.Personalities.SNILS = personalityJson.SNILS;
                personalityVersion.PartTimer = personalityJson.PartTimer;
                personalityVersion.EntityCostDMSGuid = personalityJson.EntityCostDMS;
                List<PersonalityVersion> avalible = dbSql.PersonalityVersions.Where(c => c.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null)
                                                                             .ToList();
                List<PersonalityVersion> allPersonalityVersions = dbSql.PersonalityVersions.Where(c => c.Personalities.Guid == Guid.Parse(personalityJson.personGUID) && c.VersionEndDate == null)
                                                                                           .ToList();
                _cache.Remove("personality:selectedIds");
                _cache.Remove("personality:errors");
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
        public IActionResult PersonalityVersions(string typeGuid, string newPerson, int actual, int errors)
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
            personalityVersionModel.actual = actual;
            personalityVersionModel.errors = errors;
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
        public (List<Models.MSSQL.Location.Location> locationBind, List<Models.MSSQL.Location.Location> locationLeft) GetLocationList(string guid)
        {
            // Получаем личность по GUID
            var personality = dbSql.Personalities.FirstOrDefault(x => x.Guid == Guid.Parse(guid));
            if (personality == null)
            {
                throw new ArgumentException("Personality not found");
            }
            // Получаем все локации
            var allLocation = dbSql.Locations.ToList();
            // Получаем текущую локацию личности
            var location = dbSql.PersonalityVersions.Include(x => x.Location)
                .FirstOrDefault(x => x.Personalities.Guid == personality.Guid && x.Actual == 1)?.Location;
            if (location == null)
            {
                throw new ArgumentException("Current location not found");
            }
            // Получаем GUID всех привязанных локаций
            var bindLocationGuids = dbSql.BindingPersonalityToLocation
                .Where(x => x.Personality == personality.Guid)
                .Select(x => x.Location)
                .ToList();
            // Извлекаем полные объекты Location по полученным GUID
            var bindLocations = dbSql.Locations
                .Where(loc => bindLocationGuids.Contains(loc.Guid))
                .ToList();
            // Получаем оставшиеся локации
            var locationLeft = allLocation.Except(bindLocations).ToList();
            // Возвращаем результат
            return (bindLocations, locationLeft);
        }
        [HttpPost]
        public IActionResult SaveMoreTT([FromBody] SaveMoreTTRequest request)
        {
            var result = new Result<string>();
            // Получаем все записи, которые нужно удалить
            var bindingsToDelete = dbSql.BindingPersonalityToLocation
                .Where(x => x.Personality == Guid.Parse(request.Guid))
                .ToList();
            // Удаляем старые связи
            dbSql.BindingPersonalityToLocation.RemoveRange(bindingsToDelete);
            dbSql.SaveChanges(); // Сохраняем удаление в базе перед добавлением новых записей
            if(request.Locations == null)
            {
                return Ok(result);
            }
            // Создаем новые записи
            List<BindingPersonalityToLocation> listBind = request.Locations
                .Select(loc => new BindingPersonalityToLocation
                {
                    Personality = Guid.Parse(request.Guid),
                    Location = loc
                })
                .ToList();
            // Добавляем новые связи
            dbSql.BindingPersonalityToLocation.AddRange(listBind);
            dbSql.SaveChanges(); // Сохраняем изменения
            return Ok(result);
        }
        public class SaveMoreTTRequest
        {
            public string Guid { get; set; }
            public List<Guid> Locations { get; set; }
        }
    }
}
