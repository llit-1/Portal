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
                return StatusCode(500, "Произошла ошибка при обработке запроса");
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
            string path = "\\\\fs1\\SHZWork\\Обмен2\\ПромокодыВК";

            try
            {
                if (data.Files == null || data.Files.Count == 0)
                    return BadRequest("Нет загружаемых файлов.");

                foreach (var file in data.Files)
                {
                    PromocodesVK receivedPromocodesVK = new PromocodesVK();
                    var filePath = Path.Combine(path, file.FileName);

                    // Сохранение файла
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    receivedPromocodesVK.Name = file.FileName.Split(".")[0];
                    receivedPromocodesVK.Link = "\\\\fs1\\SHZWork\\Обмен2\\ПромокодыВК\\" + file.FileName;
                    receivedPromocodesVK.CodeWord = data.CodeWord;
                    receivedPromocodesVK.StartDate = data.StartDate;
                    receivedPromocodesVK.EndDate = data.EndDate;
                    receivedPromocodesVK.Text = data.Text;
                    receivedPromocodesVK.isReusable = data.isReusable;
                    receivedPromocodesVK.Active = 1;

                    dbSql.PromocodesVK.Add(receivedPromocodesVK);
                    dbSql.SaveChanges();
                }

                return Ok("Файлы успешно загружены.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        public IActionResult HistoryCoupon()
        {
            List<ReceivedPromocodesVK> receivedPromocodes = dbSql.ReceivedPromocodesVK.Include(x => x.PromocodesVK).ToList();
             
            return PartialView(receivedPromocodes);
        }

        public IActionResult VKBOTStatistics(int days = 0)
        {
            var promo = dbSql.ReceivedPromocodesVK.Include(x => x.PromocodesVK)
                                                  .Where(x => x.Date > DateTime.Now.Date.AddDays(-days) && x.Date < DateTime.Now)
                                                  .AsEnumerable()
                                                  .GroupBy(x => x.PromocodesVK.CodeWord).ToList();

            ViewBag.Days = days;

            return PartialView(promo);
        }
    }
}


