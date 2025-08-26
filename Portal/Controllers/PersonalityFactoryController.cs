using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


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
            return PartialView();
        }

        [Authorize(Roles = "employee_control_factory")]
        public IActionResult TabMenu()
        {
            return PartialView();
        }
    }
}
