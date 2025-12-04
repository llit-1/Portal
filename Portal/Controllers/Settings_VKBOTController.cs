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

        public IActionResult VKBOT()
        {
            List<PromocodesVK> promocodes;

            promocodes = dbSql.PromocodesVK.Where(x => x.Active == 1).ToList();
            
            
            return PartialView(promocodes);
        }

        public async Task<IActionResult> DeleteNonActiveCoupons()
        {
            try
            {
                var oldCoupons = await dbSql.PromocodesVK
                    .Where(x => x.Active == 1 && x.EndDate < DateTime.Now)
                    .ToListAsync();

                if (oldCoupons.Any())
                {
                    using (var transaction = await dbSql.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            foreach (var item in oldCoupons)
                            {
                                item.Active = 0;
                            }

                            await dbSql.SaveChangesAsync();
                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }

                return Ok("Ok");
            }
            catch
            {
                return StatusCode(500, "��������� ������ ��� ��������� �������");
            }
        }

        public class CouponData
        {
            public IFormFileCollection Files { get; set; }
            public string CodeWord { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int isReusable { get; set; }
            public string Text { get; set; }

        }

        public async Task<IActionResult> UploadFiles(CouponData data)
        {
            string path = "\\\\shzhleb.ru\\shz\\SHZWork\\Обмен2\\ПромокодыВК";

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
                    receivedPromocodesVK.Link = "\\\\shzhleb.ru\\shz\\SHZWork\\Обмен2\\ПромокодыВК\\" + file.FileName;
                    receivedPromocodesVK.CodeWord = data.CodeWord;
                    receivedPromocodesVK.StartDate = data.StartDate;
                    receivedPromocodesVK.EndDate = data.EndDate;
                    receivedPromocodesVK.Text = data.Text;
                    receivedPromocodesVK.isReusable = data.isReusable;
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

        public IActionResult VKBOTStatistics(int days = 0)
        {
            var targetDate = DateTime.Now.Date.AddDays(-days);

            var receivedPromos = dbSql.ReceivedPromocodesVK
                .Include(x => x.PromocodesVK)
                .Where(x => x.Date >= targetDate && x.Date < DateTime.Now.Date.AddDays(1))
                .ToList();

            var promoStats = receivedPromos
                .GroupBy(x => x.PromocodesVK.CodeWord)
                .Select(g => new PromoStatViewModel
                {
                    CodeWord = g.Key,
                    Count = g.Count(),
                    Promocodes = g.First().PromocodesVK
                })
                .ToList();

            var activePromos = dbSql.PromocodesVK
                .Where(x => x.Active == 1)
                .ToList();

            var activeCodes = activePromos
                .GroupBy(x => x.CodeWord)
                .ToList();

            ViewBag.Days = days;

            var data = new VKBOTStatisticsViewModel
            {
                PromoStats = promoStats,
                ActiveCodes = activeCodes
            };

            return PartialView(data);
        }

        public class PromoStatViewModel
        {
            public string CodeWord { get; set; }
            public int Count { get; set; }
            public PromocodesVK Promocodes { get; set; }
        }

        public class VKBOTStatisticsViewModel
        {
            public List<PromoStatViewModel> PromoStats { get; set; }
            public List<IGrouping<string, PromocodesVK>> ActiveCodes { get; set; }
        }
    }
}


