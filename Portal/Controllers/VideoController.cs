using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Portal.Models;

namespace Portal.Controllers
{
    [Authorize(Roles = "video")]
    public class VideoController : Controller
    {
        DB.SQLiteDBContext db;
        DB.MSSQLDBContext mssql;

        public VideoController(DB.SQLiteDBContext dbContext, DB.MSSQLDBContext mssqlContext)
        {
            db = dbContext;
            mssql = mssqlContext;
        }

        // Фото с камер (фотоаналитика)
        public IActionResult Photocam()
        {
            var groups = db.CamGroups.ToList();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Просмотр фото с камер";
            log.Description = "/Video/Photocam";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(groups);
        }

        // таблица фото за выбранный период
        public IActionResult PhotoByFilters(string period01, string period02, int camgroupId)
        {
            var photos = new List<Models.MSSQL.PhotoCam>();

            var date01 = DateTime.ParseExact(period01, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var date02 = DateTime.ParseExact(period02, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            
            // получаем данные пользователя
            var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.Include(u => u.TTs).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            // фильтруем точки по правам пользователя
            var userTTIds = user.TTs.Select(t => t.Code);

            // получаем список строк из БД, но без изображения (для быстрой загрузки)
            // все группы
            if (camgroupId == 0)
            {
                var data = mssql.PhotoCams.Select(p => new
                {
                    Id = p.Id,
                    TTID = p.TTCode,
                    TTName = p.TTName,
                    camId = p.camId,
                    camName = p.camName,
                    dateTime = p.dateTime,
                    groupId = p.groupId,
                    groupName = p.groupName
                })
                    .Where(p => p.dateTime.Date >= date01.Date & p.dateTime.Date <= date02.Date & (user.AllTT | userTTIds.Contains(p.TTID))).ToList();

                foreach (var d in data)
                {
                    var photo = new Models.MSSQL.PhotoCam();
                    photo.Id = d.Id;
                    photo.TTCode = d.TTID;
                    photo.TTName = d.TTName;
                    photo.camId = d.camId;
                    photo.camName = d.camName;
                    photo.dateTime = d.dateTime;
                    photo.groupId = d.groupId;
                    photo.groupName = d.groupName;

                    photos.Add(photo);
                }
            }
            // фильтр по группе
            else
            {
                var data = mssql.PhotoCams.Select(p => new
                {
                    Id = p.Id,
                    TTID = p.TTCode,
                    TTName = p.TTName,
                    camId = p.camId,
                    camName = p.camName,
                    dateTime = p.dateTime,
                    groupId = p.groupId,
                    groupName = p.groupName
                })
                    .Where(p => p.dateTime.Date >= date01.Date & p.dateTime.Date <= date02.Date & p.groupId == camgroupId & (user.AllTT | userTTIds.Contains(p.TTID))).ToList();

                foreach (var d in data)
                {
                    var photo = new Models.MSSQL.PhotoCam();
                    photo.Id = d.Id;
                    photo.TTCode = d.TTID;
                    photo.TTName = d.TTName;
                    photo.camId = d.camId;
                    photo.camName = d.camName;
                    photo.dateTime = d.dateTime;
                    photo.groupId = d.groupId;
                    photo.groupName = d.groupName;

                    photos.Add(photo);
                }
            }
            
            return PartialView(photos);
        }

        // получаем фото из БД mssql
        public IActionResult GetPhoto(int photoId, bool resized)
        {
            FileContentResult result;

            var photo = mssql.PhotoCams.FirstOrDefault(p => p.Id == photoId).Image;
            var ms = new MemoryStream(photo);
            Image image = Image.FromStream(ms);
            

            // изменяем размер (меняет пропорции рыбьего глаза)
            if (resized)
            {
                Size newSize = new Size((int)(image.Height * 1.8), image.Height);
                var imgResized = new Bitmap(image, newSize);

               
                using (var memStream = new MemoryStream())
                {
                    imgResized.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    result = this.File(memStream.GetBuffer(), "image/jpeg");
                }
            }
            else
            {
                using (var memStream = new MemoryStream())
                {
                    image.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    result = this.File(memStream.GetBuffer(), "image/jpeg");
                }
            }            
            
            return result;
        }
    }
}
