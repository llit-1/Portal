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
using System.Threading.Tasks;

namespace Portal.Controllers
{
    [AllowAnonymous]
    public class Settings_VideoDevicesController : Controller
    {
        private DB.MSSQLDBContext dbSql;
        private IHttpClientFactory _httpClientFactory;
        public Settings_VideoDevicesController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IHttpClientFactory _httpClientFactoryConnect)
        {
            dbSql = dbSqlContext;
            _httpClientFactory = _httpClientFactoryConnect;
        }

        public IActionResult TabMenu()
        {
            return PartialView();
        }
        public IActionResult DevicesMain()
        {
            List<VideoDevices> videoDevices = dbSql.VideoDevices.Include(x => x.Location).ToList();
            return PartialView(videoDevices);
        }

        [HttpGet]
        public JsonResult GetLocationList()
        {
            List<Location> locations = dbSql.Locations.ToList();
            return Json(locations);
        }

        public IActionResult TryToConnectDevice(string ip)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ip = ip.Replace("%bkspc%", " ");
                var httpClient = _httpClientFactory.CreateClient();
                string microserviceUrl = "http://" + ip;
                HttpResponseMessage response = httpClient.GetAsync(microserviceUrl).Result;
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
            return new ObjectResult(result);
        }

        public IActionResult AddDevice(string locationGuid, string ip)
        {
            var result = new Result<string>();
            try
            {
                VideoDevices videoDevices = new VideoDevices();
                videoDevices.Location = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(locationGuid));
                videoDevices.Status = 1;
                videoDevices.Ip = ip.Trim();
                videoDevices.VideoList = "[]";
                dbSql.Add(videoDevices);
                dbSql.SaveChanges();
                result.Ok = true;
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
        }


        [AllowAnonymous]
        public IActionResult VideoList()
        {
            // разэкранирование "плюс" и "пробел"
            var result = new RKNet_Model.Result<string>();
            string path = "\\\\fs1\\SHZWork\\Обмен2\\ВидеоТВ";
                List<VideoFileInfo> list = new List<VideoFileInfo>();
                string[] allfiles = Directory.GetFiles(path);
                foreach (string filename in allfiles)
                {
                    VideoFileInfo info = new VideoFileInfo();
                    var fileInfo = new System.IO.FileInfo(filename);
                    info.FullName = fileInfo.FullName;
                    info.Name = fileInfo.Name;
                    info.SizeInMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                    list.Add(info);
                }
            return PartialView(list);
        }

        // Загрузка файла по пути
        [AllowAnonymous]
        public IActionResult GetFile(string path)
        {
            try
            {
                // разэкранирование "плюс" и "пробел"
                path = path.Replace("plustoreplace", "+");
                path = path.Replace("backspacetoreplace", " ");

                var fileInfo = new System.IO.FileInfo(path);
                var file = System.IO.File.ReadAllBytes(path);

                switch (fileInfo.Extension.ToLower())
                {
                    case ".avi":
                        return new FileContentResult(file, "video/avi");
                    case ".mov":
                        return new FileContentResult(file, "video/quicktime");
                    case ".mp4":
                        return new FileContentResult(file, "video/mp4");
                    case ".mpeg":
                        return new FileContentResult(file, "video/mpeg");
                    default:
                        return new FileContentResult(file, "application/octet-stream");

                }
            }
            catch (Exception e)
            {
                return new ObjectResult(e.Message);
            }
        }

        public class VideoFileInfo
        {
            public int Position { get; set; }
            public string FullName { get; set; }
            public string Name { get; set; }
            public double SizeInMB { get; set; }
        }

        [AllowAnonymous]
        public List<VideoFileInfo> GetVideoListForTv()
        {
            string path = "\\\\fs1\\SHZWork\\Обмен2\\ВидеоТВ";
            // разэкранирование "плюс" и "пробел"
            path = path.Replace("plustoreplace", "+");
            path = path.Replace("backspacetoreplace", " ");
            List<VideoFileInfo> list = new List<VideoFileInfo>();
                string[] allfiles = Directory.GetFiles(path);
                foreach (string filename in allfiles)
                {
                    VideoFileInfo info = new VideoFileInfo();
                    var fileInfo = new System.IO.FileInfo(filename);
                    info.Position = dbSql.VideoInfo.Count() + 1;
                    info.FullName = fileInfo.FullName;
                    info.Name = fileInfo.Name;
                    list.Add(info);
                }

            return list;
        }

        public async Task<IActionResult> UploadVideo(IFormFile videoFile)
        {
            string path = "\\\\fs1\\SHZWork\\Обмен2\\ВидеоТВ";

            if (videoFile == null || videoFile.Length == 0)
            {
                return Json(new { message = "No file selected" });
            }

            try
            {
                var filePath = Path.Combine(path, videoFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await videoFile.CopyToAsync(stream);
                }

                VideoInfo info = new VideoInfo();
                info.Name = videoFile.FileName;
                info.Path = filePath;
                dbSql.VideoInfo.Add(info);
                dbSql.SaveChanges();

                return Json(new { message = "File uploaded successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { message = $"Error uploading file: {ex.Message}" });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetVideoForTv(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("URL parameter is missing");
            }

            try
            {
                // Разэкранирование URL
                string filePath = url.Replace("plustoreplace", "+").Replace("backspacetoreplace", " ");

                // Получение информации о файле
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    return NotFound("File not found");
                }

                // Чтение файла в байты
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var fileExtension = fileInfo.Extension.ToLower();

                // Определение MIME-типа файла
                string mimeType = fileExtension switch
                {
                    ".mp4" => "video/mp4",
                    ".avi" => "video/x-msvideo",
                    ".mov" => "video/quicktime",
                    ".mpeg" => "video/mpeg",
                    _ => "application/octet-stream"
                };

                // Возвращение файла в ответе
                return File(fileBytes, mimeType, fileInfo.Name);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}


