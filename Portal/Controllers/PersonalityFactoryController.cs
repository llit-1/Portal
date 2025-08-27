using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL.Factory;
using System.Collections.Generic;
using System.Linq;


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
            return PartialView(personalityFactoryAddModel);
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult GetWorkshops(int depertment)
        {
           List<FactoryWorkshop> factoryWorkshops = dbSql.FactoryDepartmentFactoryWorkshop
                                                          .Include(x => x.FactoryWorkshop)
                                                          .Where(x => x.FactoryDepartmentId == depertment)
                                                          .Select(x => x.FactoryWorkshop).ToList();
            return Ok(factoryWorkshops);
        }

        public IActionResult GetJobTitles(int depertment, int workshop)
        {
            List<FactoryJobTitle> factoryJobTitles = dbSql.FactoryDepartmentWorkshopJobTitle
                                                           .Include(x => x.JobTitleWorkshop)
                                                           .ThenInclude(a => a.FactoryJobTitle)
                                                           .Where(x => x.FactoryDepartmentId == depertment && x.FactoryWorkshopId == workshop)
                                                           .Select(x => x.JobTitleWorkshop.FactoryJobTitle).ToList();
            return Ok(factoryJobTitles);
        }
        public IActionResult GetCitizenship(int citizenshipType)
        {
            List<FactoryCitizenship> factoryCitizenships = dbSql.FactoryCitizenship.Where(x => x.CitizenshipTypeId == citizenshipType).ToList();
            return Ok(factoryCitizenships);
        }

        public IActionResult SaveNewPerson([FromBody] FactoryPerson person)
        {
            if (person == null)
            {
                return BadRequest(new { Message = "person is null" });
            }
            dbSql.FactoryPerson.Add(person);
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
        }
    }
}
