using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Portal.Models;
using Portal.Models.MSSQL.Factory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Portal.Controllers.PersonalityFactoryController;

namespace Portal.Controllers
{
    public class PersonalityFactoryController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        public PersonalityFactoryController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }       

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult PersonalityFactoryTable()
        {
            List<FactoryPerson> Factorypersons = dbSql.FactoryPerson.Include(c=>c.Bank)
                                                                    .Include(c=>c.Citizenship)
                                                                    .Include(c=>c.Entity)
                                                                    .Include(c=>c.DocumentType)
                                                                    .Include(c=>c.DepartmentWorkshopJobTitle)
                                                                    .ThenInclude(a=>a.JobTitleWorkshop)
                                                                    .ThenInclude(b=>b.FactoryJobTitle)
                                                                    .Include(c => c.DepartmentWorkshopJobTitle)
                                                                    .ThenInclude(a => a.JobTitleWorkshop)
                                                                    .ThenInclude(b => b.FactoryWorkshop)
                                                                    .Include(c => c.DepartmentWorkshopJobTitle)
                                                                    .ThenInclude(a => a.DepartmentWorkshop)
                                                                    .ThenInclude(b => b.FactoryDepartment)
                                                                    .Where(x => x.Fake == null)
                                                                    .ToList();
            return PartialView(Factorypersons);
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult TabMenu()
        {
            return PartialView();
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult PersonalityFactoryAdd()
        {
            PersonalityFactoryAddModel personalityFactoryAddModel = new PersonalityFactoryAddModel();
            personalityFactoryAddModel.FactoryDepartments = dbSql.FactoryDepartment.ToList();
            personalityFactoryAddModel.FactoryEntities = dbSql.FactoryEntity.ToList();
            personalityFactoryAddModel.FactoryCitizenshipTypes = dbSql.FactoryCitizenshipType.ToList();
            personalityFactoryAddModel.FactoryDocumentTypes = dbSql.FactoryDocumentType.ToList();
            personalityFactoryAddModel.FactoryBanks = dbSql.FactoryBanks.ToList();
            personalityFactoryAddModel.FactorySKUDGroups = dbSql.FactorySKUDGroup.ToList();
            return PartialView(personalityFactoryAddModel);
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult GetWorkshops(int depertment)
        {
           List<FactoryWorkshop> factoryWorkshops = dbSql.FactoryDepartmentFactoryWorkshop
                                                          .Include(x => x.FactoryWorkshop)
                                                          .Where(x => x.FactoryDepartmentId == depertment)
                                                          .Select(x => x.FactoryWorkshop).OrderBy(x => x.Name).ToList();
            return Ok(factoryWorkshops);
        }

        public IActionResult GetJobTitles(int depertment, int workshop)
        {
            List<FactoryJobTitle> factoryJobTitles = dbSql.FactoryDepartmentWorkshopJobTitle
                                                           .Include(x => x.JobTitleWorkshop)
                                                           .ThenInclude(a => a.FactoryJobTitle)
                                                           .Where(x => x.FactoryDepartmentId == depertment && x.FactoryWorkshopId == workshop)
                                                           .Select(x => x.JobTitleWorkshop.FactoryJobTitle).OrderBy(x => x.Name).ToList();
            return Ok(factoryJobTitles);
        }
        public IActionResult GetCitizenship(int citizenshipType)
        {
            List<FactoryCitizenship> factoryCitizenships = dbSql.FactoryCitizenship.Where(x => x.CitizenshipTypeId == citizenshipType).OrderBy(x => x.Name).ToList();
            return Ok(factoryCitizenships);
        }


        public async Task<IActionResult> SaveNewPerson([FromBody] FactoryPerson person)
        {
            if (person == null)
                return BadRequest(new { Message = "person is null" });

            if (dbSql.FactoryPerson.Any(x => x.Passport == person.Passport))
                return BadRequest(new { Message = "Пользователь с таким паспортом уже зарегистрирован" });

            // Сохраняем фото если есть
            if (string.IsNullOrEmpty(person.Photo))
            {
                //person.Photo = await SavePhotoAsync(person.Photo, person.PassCardNumber);
                person.Photo = null;
            }

            dbSql.FactoryPerson.Add(person);
            await dbSql.SaveChangesAsync();

            return Ok();
        }

        public byte[] ConvertDataUrlToByteArray(string dataUrl)
        {
            var base64Data = dataUrl.Split(',')[1];
            return Convert.FromBase64String(base64Data);
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult PersonalityFactoryEdit(int id)
        {

            PersonalityFactoryEditModel personalityFactoryEditModel = new PersonalityFactoryEditModel();
            personalityFactoryEditModel.FactoryPerson = dbSql.FactoryPerson.Include(x => x.Citizenship).FirstOrDefault(x => x.Id == id);
            personalityFactoryEditModel.FactoryDepartments = dbSql.FactoryDepartment.ToList();
            personalityFactoryEditModel.FactoryWorkshops = dbSql.FactoryDepartmentFactoryWorkshop
                                                          .Include(x => x.FactoryWorkshop)
                                                          .Where(x => x.FactoryDepartmentId == personalityFactoryEditModel.FactoryPerson.FactoryDepartment)
                                                          .Select(x => x.FactoryWorkshop).ToList();
            personalityFactoryEditModel.FactoryJobTitles = dbSql.FactoryDepartmentWorkshopJobTitle
                                                           .Include(x => x.JobTitleWorkshop)
                                                           .ThenInclude(a => a.FactoryJobTitle)
                                                           .Where(x => x.FactoryDepartmentId == personalityFactoryEditModel.FactoryPerson.FactoryDepartment && x.FactoryWorkshopId == personalityFactoryEditModel.FactoryPerson.FactoryWorkshop)
                                                           .Select(x => x.JobTitleWorkshop.FactoryJobTitle).ToList();
            personalityFactoryEditModel.FactoryCitizenshipType = dbSql.FactoryCitizenshipType.FirstOrDefault(x => x.Id == personalityFactoryEditModel.FactoryPerson.Citizenship.CitizenshipTypeId);
            personalityFactoryEditModel.FactoryCitizenships = dbSql.FactoryCitizenship.Where(x => x.CitizenshipTypeId == personalityFactoryEditModel.FactoryCitizenshipType.Id).ToList();
            personalityFactoryEditModel.FactoryEntities = dbSql.FactoryEntity.ToList();
            personalityFactoryEditModel.FactoryDocumentTypes = dbSql.FactoryDocumentType.ToList();
            personalityFactoryEditModel.FactoryBanks = dbSql.FactoryBanks.ToList();
            personalityFactoryEditModel.FactoryCitizenshipTypes = dbSql.FactoryCitizenshipType.ToList();

            personalityFactoryEditModel.FactorySKUDGroups = dbSql.FactorySKUDGroup.ToList();

            return PartialView(personalityFactoryEditModel);
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult EditPerson([FromBody] FactoryPerson person)
        {
            if (person == null)
            {
                return BadRequest(new { Message = "person is null" });
            }

            if (!dbSql.FactoryPerson.Any(x => x.Id == person.Id))
                return BadRequest(new { Message = "invalid person id" });

            if (dbSql.FactoryPerson.Any(x => x.Passport == person.Passport && x.Id != person.Id))
                return BadRequest(new { Message = "Паспорт уже зарегистрирован у другого человека" });


            dbSql.Entry(person).State = EntityState.Modified;
            dbSql.SaveChanges();
            return Ok();
        }




        public class PersonalityFactoryAddModel
        {
            public List<FactoryDepartment> FactoryDepartments { get; set; } = new List<FactoryDepartment>();
            public List<FactoryEntity> FactoryEntities { get; set; } = new List<FactoryEntity>();
            public List<FactoryCitizenshipType> FactoryCitizenshipTypes { get; set; } = new List<FactoryCitizenshipType>();
            public List<FactoryDocumentType> FactoryDocumentTypes { get; set; } = new List<FactoryDocumentType>();
            public List<FactoryBanks> FactoryBanks { get; set; } = new List<FactoryBanks>();
            public List<FactorySKUDGroup> FactorySKUDGroups { get; set; } = new List<FactorySKUDGroup>();
        }

        public class PersonalityFactoryEditModel
        {
            public FactoryPerson FactoryPerson { get; set; }
            public List<FactoryDepartment> FactoryDepartments { get; set; } = new List<FactoryDepartment>();
            public List<FactoryWorkshop> FactoryWorkshops { get; set; }
            public List<FactoryJobTitle> FactoryJobTitles { get; set; }
            public List<FactoryCitizenshipType> FactoryCitizenshipTypes { get; set; }
            public FactoryCitizenshipType FactoryCitizenshipType { get; set; }
            public List<FactoryEntity> FactoryEntities { get; set; } = new List<FactoryEntity>();
            public List<FactoryCitizenship> FactoryCitizenships { get; set; } = new List<FactoryCitizenship>();
            public List<FactoryDocumentType> FactoryDocumentTypes { get; set; } = new List<FactoryDocumentType>();
            public List<FactoryBanks> FactoryBanks { get; set; } = new List<FactoryBanks>();
            public List<FactorySKUDGroup> FactorySKUDGroups { get; set; } = new List<FactorySKUDGroup>();

        }
    }
}
