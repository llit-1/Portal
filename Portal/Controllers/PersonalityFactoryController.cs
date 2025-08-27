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
    }
}
