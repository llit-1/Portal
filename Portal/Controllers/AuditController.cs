using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model.Audit;
using Microsoft.EntityFrameworkCore;


namespace Portal.Controllers
{
    public class AuditController : Controller
    {
        DB.SQLiteDBContext db;
        DB.MSSQLDBContext mssql;
        
        public AuditController(DB.SQLiteDBContext sqliteContext, DB.MSSQLDBContext mssqlContext)
        {
            db = sqliteContext;
            mssql = mssqlContext;
        }

        public IActionResult Index()
        {                   
            return PartialView();
        }

        public IActionResult Settings()
        {
            // получаем список объектов адуита пекарен (id == 1)
            var BakeryItems = db.AuditItems.Include(s => s.Scores).Include(i => i.Items).FirstOrDefault(i => i.Id == 1).Items;
            return PartialView(BakeryItems);
        }

        // получаем элемент аудита с коллекциями
        public IActionResult GetItem(int itemId)
        {
            var item = db.AuditItems.Include(i => i.Scores).Include(i => i.Items).FirstOrDefault(i => i.Id == itemId);
            return new ObjectResult(item);
        }
    }
}
