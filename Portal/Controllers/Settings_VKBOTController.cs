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

        public class CouponData
        {
            public IFormFileCollection Files { get; set; }
            public string CodeWord { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public async Task<IActionResult> UploadFiles(CouponData data)
        {
            string path = "\\\\fs1\\SHZWork\\�����2\\�����������";

            try
            {
                if (data.Files == null || data.Files.Count == 0)
                    return BadRequest("��� ����������� ������.");

                foreach (var file in data.Files)
                {
                    PromocodesVK receivedPromocodesVK = new PromocodesVK();
                    var filePath = Path.Combine(path, file.FileName);

                    // ���������� �����
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    receivedPromocodesVK.Name = file.FileName.Split(".")[0];
                    receivedPromocodesVK.Link = "\\\\fs1\\SHZWork\\�����2\\�����������\\" + file.FileName;
                    receivedPromocodesVK.CodeWord = data.CodeWord;
                    receivedPromocodesVK.StartDate = data.StartDate;
                    receivedPromocodesVK.EndDate = data.EndDate;
                    receivedPromocodesVK.Active = 1;

                    dbSql.PromocodesVK.Add(receivedPromocodesVK);
                    dbSql.SaveChanges();
                }

                return Ok("����� ������� ���������.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"���������� ������ �������: {ex.Message}");
            }
        }

        public IActionResult HistoryCoupon()
        {
            List<ReceivedPromocodesVK> receivedPromocodes = dbSql.ReceivedPromocodesVK.Include(x => x.PromocodesVK).ToList();
             
            return PartialView(receivedPromocodes);
        }
    }
}

