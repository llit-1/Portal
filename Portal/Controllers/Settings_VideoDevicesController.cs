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
            List<Location> locations = dbSql.Locations.OrderBy(x => x.Name).ToList();
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
                    info.Guid = dbSql.VideoInfo.FirstOrDefault(x => x.Name == fileInfo.Name).Guid;
                    info.Position = dbSql.VideoInfo.FirstOrDefault(x => x.Name == fileInfo.Name).Position;
                    if(info.Guid == null || info.Position == null)
                    {
                        continue;
                    }
                    info.FullName = fileInfo.FullName;
                    info.Name = fileInfo.Name;
                    info.SizeInMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                    list.Add(info);
                }
            var totallist = list.OrderBy(x => x.Position).ToList();
            return PartialView(totallist);
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
            public Guid Guid { get; set; }
            public int Position { get; set; }
            public string FullName { get; set; }
            public string Name { get; set; }
            public double SizeInMB { get; set; }
        }

        [AllowAnonymous]
[HttpGet]
        public IActionResult GetVideoListForTv()
        {
            try
            {
                List<VideoInfo> info = dbSql.VideoInfo.OrderBy(x => x.Position).ToList();
                if (info.Count == 0)
                {
                    return Json(new { message = "No video info available" });
                }
                else
                {
                    return Json(info);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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

                foreach (var item in dbSql.VideoInfo.ToList())
                {
                    item.Position += 1;
                }
                VideoInfo info = new VideoInfo();
                info.Position = 0;
                info.Name = videoFile.FileName;
                info.Path = filePath;
                dbSql.VideoInfo.Add(info);
                dbSql.SaveChanges();
                UpdateDataOnDevices();
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

        public IActionResult DeleteDevice(string guid)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                var device = dbSql.VideoDevices.FirstOrDefault(x => x.Guid == Guid.Parse(guid));
                if (device == null)
                {
                    result.Ok = false;
                    result.ErrorMessage = "Device not found";
                    return new ObjectResult(result);
                }
                dbSql.VideoDevices.Remove(device);
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

        public IActionResult SwapPosition(string guid, int newposition)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                var oldpos = dbSql.VideoInfo.FirstOrDefault(x => x.Position == newposition).Position = dbSql.VideoInfo.FirstOrDefault(x => x.Guid == Guid.Parse(guid)).Position;
                var npos = dbSql.VideoInfo.FirstOrDefault(x => x.Guid == Guid.Parse(guid)).Position = newposition;
                dbSql.SaveChanges();
                UpdateDataOnDevices();
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
        [HttpPost]
        public async Task<IActionResult> UpdateDataOnDevices()
        {
            List<VideoDevices> devices = dbSql.VideoDevices.ToList();
            var results = new List<string>();

            foreach (var device in devices)
            {
                if (!string.IsNullOrEmpty(device.Ip))
                {
                    var result = await SendRequestToDeviceAsync(device.Ip);
                    results.Add(result);
                }
            }

            return Ok(results);
        }

        private async Task<string> SendRequestToDeviceAsync(string ip)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var requestUrl = $"https://{ip}/?action=update";
                var response = await httpClient.GetAsync(requestUrl);

                if (response.IsSuccessStatusCode)
                {
                    return $"Success: {ip}";
                }
                else
                {
                    return $"Failed: {ip} - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ip} - {ex.Message}";
            }
        }

        public IActionResult DeleteVideo(string guid) 
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                string path = "\\\\fs1\\SHZWork\\Обмен2\\ВидеоТВ";
                VideoInfo videoInfo = dbSql.VideoInfo.FirstOrDefault(x => x.Guid == Guid.Parse(guid));
                if(videoInfo != null) 
                {
                    if (System.IO.File.Exists(path + "\\" + videoInfo.Name))
                    {
                        System.IO.File.Delete(path + "\\" + videoInfo.Name);
                    }

                    foreach (var item in dbSql.VideoInfo.ToList())
                    {
                        if(item.Position > videoInfo.Position)
                        {
                            item.Position -= 1;
                        }
                    }

                    dbSql.Remove(videoInfo);
                    dbSql.SaveChanges();

                    UpdateDataOnDevices();

                    result.Ok = true;
                    return new OkObjectResult(result);
                } else
                {
                    result.Ok = false;
                    return new ObjectResult(result);
                }  
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
        }
    }
}

