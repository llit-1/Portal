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
using System.Diagnostics;
using static module_NX.NX.FullInfo;

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
            Portal.Models.JsonModels.DeviceMainJson deviceMainJson = new Models.JsonModels.DeviceMainJson();
            deviceMainJson.videoDevices = dbSql.VideoDevices.Include(x => x.Location).Include(x => x.Orientation).ToList(); ;
            deviceMainJson.videoOrientation = dbSql.VideoOrientation.ToList();
            return PartialView(deviceMainJson);
        }

        [HttpGet]
        public IActionResult GetLocationList()
        {
            List<VideoInfo> videoInfos = dbSql.VideoInfo.OrderBy(x => x.Name).ToList();
            List<Location> locations = dbSql.Locations.OrderBy(x => x.Name).ToList();
            var model = Tuple.Create(videoInfos, locations);
            return Json(model);
        }

        public async Task<IActionResult> TryToConnectDevice(string ip)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ip = ip.Replace("%bkspc%", " ");
                var httpClient = _httpClientFactory.CreateClient();
                string microserviceUrl = $"http://{ip}/?action=getIP";
                HttpResponseMessage response = await httpClient.GetAsync(microserviceUrl);
                string currentVersion = GetActualVersion().Split(".apk")[0];
                string responseString = await response.Content.ReadAsStringAsync();

                if (responseString != currentVersion)
                {
                    string request = $"http://{ip}/?action=download&param1={currentVersion}";
                    HttpResponseMessage res = await httpClient.GetAsync(request);
                }
                else
                {
                    result.Data = "Устройство в актуальной версии";
                }

                result.Ok = true;
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
            }
            return new ObjectResult(result);
        }


        [AllowAnonymous]
        public string GetActualVersion()
        {
            // разэкранирование "плюс" и "пробел"
            var result = new RKNet_Model.Result<string>();
            result.Ok = false;
            string path = "\\\\fs1\\SHZWork\\Обмен2\\ВидеоТВ";
            string[] allfiles = Directory.GetFiles(path);
            foreach (string filename in allfiles)
            {
                var fileInfo = new System.IO.FileInfo(filename);
                if (fileInfo.Name.EndsWith(".apk"))
                {
                    result.Ok = true;
                    return fileInfo.Name;
                } else
                {
                    continue;
                }
                
            }
            return "Error";
        }

        [AllowAnonymous]
        public IActionResult GetAPK(string filename)
        {
            // разэкранирование "плюс" и "пробел"
            string path = "\\\\fs1\\SHZWork\\Обмен2\\ВидеоТВ";
            string filePath = path + "\\" + filename + ".apk";
            // Получение информации о файле
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                return NotFound("File not found");
            }

            byte[] fileBytes;
            try
            {
                fileBytes = System.IO.File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            return File(fileBytes, "application/vnd.android.package-archive", fileInfo.Name);
        }


        public IActionResult AddDevice(string locationGuid, string ip, string arr)
        {
            var result = new Result<string>();
            try
            {
                VideoDevices videoDevices = new VideoDevices();
                videoDevices.Location = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(locationGuid));
                videoDevices.Status = 1;
                videoDevices.Ip = ip.Trim();
                videoDevices.Orientation = dbSql.VideoOrientation.FirstOrDefault(x => x.Number == 0);
                videoDevices.VideoList = arr.Trim();
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
                    if(fileInfo.Name.EndsWith(".apk"))
                    {
                        continue;
                    }
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
        public IActionResult GetVideoListForTv(string ip)
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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetVideoListArray(string ip)
        {
            try
            {
                VideoDevices info = dbSql.VideoDevices.Include(x => x.Location).FirstOrDefault(x => x.Ip == ip);
                if (info == null)
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



        public async Task<IActionResult> SwapPosition(string guid, int newposition)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                var oldpos = dbSql.VideoInfo.FirstOrDefault(x => x.Position == newposition).Position = dbSql.VideoInfo.FirstOrDefault(x => x.Guid == Guid.Parse(guid)).Position;
                var npos = dbSql.VideoInfo.FirstOrDefault(x => x.Guid == Guid.Parse(guid)).Position = newposition;
                dbSql.SaveChanges();
                await UpdateDataOnDevices();
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

        public async Task<IActionResult> UpdateDataOnDevices()
        {
            List<VideoDevices> devices = dbSql.VideoDevices.ToList();
            var results = new List<string>();

            foreach (var device in devices)
            {
                if (!string.IsNullOrEmpty(device.Ip))
                {
                    var result = await SendRequestToDeviceAsync(device.Ip.Trim());
                    results.Add(result);
                }
            }

            return Ok(results);
        }

        public async Task<string> SendRequestToDeviceAsync(string ip)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ip = ip.Replace("%bkspc%", " ");
                var httpClient = _httpClientFactory.CreateClient();
                string microserviceUrl = "http://" + ip + "?action=update";
                HttpResponseMessage response = await httpClient.GetAsync(microserviceUrl);

                if (response.IsSuccessStatusCode)
                {
                    result.Ok = true;
                    result.Data = "Success: " + ip;
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = $"Failed: {ip} - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = $"Error: {ip} - {ex.Message}";
            }
            return result.Ok.ToString();
        }

        public IActionResult EditCustomVideoArray(string VideoName)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                List<VideoDevices> videoDevices = dbSql.VideoDevices.ToList();
                foreach (var videoDevice in videoDevices)
                {
                    if (!string.IsNullOrEmpty(videoDevice.VideoList) && videoDevice.VideoList != "[]")
                    {
                        List<string> list = ConvertToList(videoDevice.VideoList);
                        if (list.Contains(VideoName.Trim()))
                        {
                            list.Remove(VideoName.Trim());
                            videoDevice.VideoList = JsonSerializer.Serialize(list).Replace("\"", "");
                        }
                    }
                }
                dbSql.SaveChanges();

                result.Ok = true;
                return new ObjectResult(result);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                return new ObjectResult(result);
            }
        }

        private List<string> ConvertToList(string input)
        {
            // Убираем квадратные скобки и делим строку на элементы
            input = input.Trim('[', ']');
            List<string> list = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim(' ', '"'))
                                     .ToList();
            return list;
        }



        public async Task<IActionResult> DeleteVideo(string guid) 
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

                    EditCustomVideoArray(videoInfo.Name);
                    await UpdateDataOnDevices();

                    result.Ok = true;
                    return new ObjectResult(result);
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
                return new ObjectResult(result); ;
            }
        }

        public async Task<IActionResult> ReloadDevice(string ip)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ip = ip.Replace("%bkspc%", " ");
                var httpClient = _httpClientFactory.CreateClient();
                string microserviceUrl = "http://" + ip + "?action=reload";
                HttpResponseMessage response = await httpClient.GetAsync(microserviceUrl);

                if (response.IsSuccessStatusCode)
                {
                    result.Ok = true;
                    result.Data = "Success: " + ip;
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = $"Failed: {ip} - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = $"Error: {ip} - {ex.Message}";
            }
            return new ObjectResult(result);
        }

        public async Task<IActionResult> GetScreenshot(string ip)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ip = ip.Replace("%bkspc%", " ");
                var httpClient = _httpClientFactory.CreateClient();
                string microserviceUrl = "http://" + ip + "?action=screen";
                HttpResponseMessage response = await httpClient.GetAsync(microserviceUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsByteArrayAsync();
                    return File(content, "image/png");
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = $"Failed: {ip} - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = $"Error: {ip} - {ex.Message}";
            }
            return new ObjectResult(result);
        }

        public async Task<IActionResult> SelectOrientation(string value, string ip)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ip = ip.Replace("%bkspc%", " ");
                var httpClient = _httpClientFactory.CreateClient();
                var orientationGuid = dbSql.VideoOrientation.FirstOrDefault(x => x.Guid == Guid.Parse(value));
                string microserviceUrl = "http://" + ip + "?action=orientation&param1=" + orientationGuid.Number;
                HttpResponseMessage response = await httpClient.GetAsync(microserviceUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    
                    var orientation = dbSql.VideoDevices.FirstOrDefault(x => x.Ip == ip.Trim()).Orientation = orientationGuid;
                    result.Ok = true;
                    result.Data = "Success: " + ip;
                    dbSql.SaveChanges();
                }
                else
                {
                    result.Ok = false;
                    result.ErrorMessage = $"Failed: {ip} - {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = $"Error: {ip} - {ex.Message}";
            }
            return new ObjectResult(result);
        }

        [HttpGet]
        public IActionResult GetLocationListWithGuid(string guid)
        {
            List<VideoInfo> videoInfos = dbSql.VideoInfo.OrderBy(x => x.Name).ToList();
            List<Location> locations = dbSql.Locations.OrderBy(x => x.Name).ToList();
            VideoDevices videoDevices = dbSql.VideoDevices.FirstOrDefault(x => x.Guid == Guid.Parse(guid));
            var model = Tuple.Create(videoInfos, locations, videoDevices);
            return Json(model);
        }
        public IActionResult SaveDevice(string locationGuid, string ip, string arr)
        {
            var result = new Result<string>();
            try
            {
                VideoDevices videoDevices = dbSql.VideoDevices.Include(x => x.Location).Include(x => x.Orientation).FirstOrDefault(x => x.Ip == ip.Trim());
                videoDevices.Location = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(locationGuid));
                videoDevices.Status = 1;
                videoDevices.Ip = ip.Trim();
                videoDevices.VideoList = arr.Trim();
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
        public IActionResult CheckDeviceIp(string ip)
        {
            var result = new Result<string>();
            try
            {
                VideoDevices video = dbSql.VideoDevices.FirstOrDefault(x => x.Ip == (ip.Trim() + ":8080"));
                if(video == null)
                {
                    VideoDevices videoDevices = new();
                    videoDevices.Location = dbSql.Locations.FirstOrDefault();
                    videoDevices.Status = 1;
                    videoDevices.Ip = ip.Trim() + ":8080";
                    videoDevices.VideoList = "[" + dbSql.VideoInfo.LastOrDefault().Name + "]";
                    videoDevices.Orientation = dbSql.VideoOrientation.FirstOrDefault(x => x.Number == 0);
                    dbSql.Add(videoDevices);
                    dbSql.SaveChanges();
                    result.Ok = true;
                    return new OkObjectResult(result);
                }
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
        public async Task RebootDevices()
        {
            // ������ IP-������� ���������
            List<VideoDevices> videoDevices = dbSql.VideoDevices.ToList();

            // ������ ��������� ���� ��������� �����������
            List<Task> tasks = new List<Task>();

            foreach (var ip in videoDevices)
            {
                tasks.Add(ProcessDeviceAsync(ip.Ip.Trim()));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("��� ���������� ����������.");
        }

        static async Task ProcessDeviceAsync(string ip)
        {
            try
            {
                Console.WriteLine($"[START] ����������: {ip}");
                await ExecuteAdbCommandAsync($"kill-server");
                await ExecuteAdbCommandAsync($"start-server");
                await ExecuteAdbCommandAsync($"connect {ip}");
                await ExecuteAdbCommandAsync("reboot");
                Console.WriteLine($"[SUCCESS] ����������: {ip}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ����������: {ip} - {ex.Message}");
            }
        }

        static async Task ExecuteAdbCommandAsync(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            Console.WriteLine(output);
        }

    }
}


