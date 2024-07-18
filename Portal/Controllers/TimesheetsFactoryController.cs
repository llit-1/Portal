/*using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Location;
using RKNet_Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Portal.Controllers
{
    public class TimesheetsFactoryController : Controller
    {
        private DB.MSSQLDBContext dbSql;
        private IHttpClientFactory _httpClientFactory;
        public TimesheetsFactoryController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IHttpClientFactory _httpClientFactoryConnect)
        {
            dbSql = dbSqlContext;
            _httpClientFactory = _httpClientFactoryConnect;
        }

        public IActionResult TimesheetsMain()
        {
            List<TimesheetsFactory> TimesheetsFactory = dbSql.TimesheetsFactory.Include(x => x.Location).Include(x => x.Personality).Include(x => x.Entity).ToList();
            return PartialView(TimesheetsFactory);
        }
    }
}


*/