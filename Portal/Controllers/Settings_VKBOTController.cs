using DocumentFormat.OpenXml.Presentation;
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
using Portal.Models.MSSQL.PersonalityVersions;
using Newtonsoft.Json;
using Portal.Models.JsonModels;
using static Portal.Controllers.TimesheetsFactoryController;
using Portal.Models.MSSQL.Personality;
using System.Globalization;

namespace Portal.Controllers
{
    public class Settings_VKBOTController : Controller
    {
        private DB.MSSQLDBContext dbSql;
        private IHttpClientFactory _httpClientFactory;
        private DB.SQLiteDBContext db;
        public Settings_VKBOTController (DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IHttpClientFactory _httpClientFactoryConnect)
        {
            dbSql = dbSqlContext;
            db = context;
            _httpClientFactory = _httpClientFactoryConnect;
        }

        public IActionResult TabMenu()
        {
            return PartialView();
        }

        public IActionResult VKBOT(int actual)
        {
            List<PromocodesVK> promocodes;

            if (actual == 1)
            {
                promocodes = dbSql.PromocodesVK.ToList();
            } else
            {
                promocodes = dbSql.PromocodesVK.Where(x => x.Active == 1).ToList();
            }
            
            return PartialView(promocodes);
        }

        public IActionResult HistoryCoupon()
        {
            List<ReceivedPromocodesVK> receivedPromocodes = dbSql.ReceivedPromocodesVK.ToList();
             
            return PartialView(receivedPromocodes);
        }
    }
}


