using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net.Http.Headers;
using RKNet_Model;
using RKNet_Model.VMS;
using RKNet_Model.VMS.NX;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using IdentityModel;
using DocumentFormat.OpenXml.Math;


namespace Portal.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        public ApiController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }

        // Версия Api
        [HttpGet]
        public IActionResult Idex()
        {
            return new ObjectResult("RKNet Web Api" + " v" + Portal.Models.SettingsInternal.portalVersion);
        }

        // Состояние аутентификации пользователя (True или False)
        [HttpGet("UserState")]
        public IActionResult UserState()
        {
            return new ObjectResult(User.Identity.IsAuthenticated.ToString());
        }

        // NX ---------------------------------------------------------------------------------------------------------------------
        
        // Список серверов и камер внутри системы
        [HttpGet("GetNxServers/{systemName}")]
        public IActionResult GetNxServers(string systemName)
        {
            var result = new Result<IEnumerable<module_NX.NX.FullInfo.server>>();
            try
            {
                var nx = new module_NX.NX();
                var nxSystem = db.NxSystems.First(c => c.Name == systemName);

                var fullInfo = nx.GetFullInfo(nxSystem);
                if (fullInfo.Ok)
                {
                    var servers = fullInfo.Data.servers.OrderBy(s => s.name).ToList();
                    // Убираем NX-Abramova21 из списка
                    try
                    {
                        var servToRemove = servers.First(s => s.name == "NX-Abramova21");
                        servers.Remove(servToRemove);
                    }
                    catch { }

                    var cameras = fullInfo.Data.cameraUserAttributesList;

                    foreach (var serv in servers)
                    {
                        serv.cameras.AddRange(cameras.Where(c => c.preferredServerId == serv.id));
                        serv.sysName = nxSystem.Name;
                    }                   

                    result.Data = servers;

                    return new ObjectResult(result);
                }
                else
                {
                    result.Ok = false;
                    result.Title = fullInfo.Title;
                    result.ErrorMessage = fullInfo.ErrorMessage;
                    result.ExceptionText = fullInfo.ExceptionText;
                    return new ObjectResult(result);
                }
            }
            catch (Exception e)
            {                
                result.Title = "Ошибка запроса списка серверов на системе NX: ApiController -> GetNxServers";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return PartialView("Error", result);
            }
        }

        // Запрос картинки с камеры
        [HttpGet("GetImages/{jsn}")]
        public IActionResult GetImages(string jsn)
        {
            try
            {
                jsn = jsn.Replace("pp", " ");
                var camView = JsonConvert.DeserializeObject<ViewModels.CameraView>(jsn);

                var dateTime = new DateTime();
                try
                {
                    dateTime = DateTime.ParseExact(camView.dateTime, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    dateTime = DateTime.Now;
                }

                var nxSystem = db.NxSystems.First(c => c.Name == camView.sysName);
                var nx = new module_NX.NX();

                var nxCam = new NxCamera();
                //nxCam.NxSystem = nxSystem;
                //nxCam.Guid = camView.cameraGuid;

                //var getCamPicture = nx.GetCameraPicture(dateTime, nxCam, camView.previewHeight);
                if (false)//getCamPicture.Ok)
                {
                    //var cameraBitmap = getCamPicture.Data;
                    FileContentResult result;

                    using (var memStream = new MemoryStream())
                    {
                        //cameraBitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        result = this.File(memStream.GetBuffer(), "image/jpeg");
                    }
                    return result;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        // Сохранение зоны с камеры
        [HttpGet("SaveZone/{jsn}")]
        public IActionResult SaveZone(string jsn)
        {
            var result = new Result<string>();
            
            try
            {
                jsn = jsn.Replace("pp", " ");
                var cameraView = JsonConvert.DeserializeObject<ViewModels.CameraView>(jsn);
                var zones = cameraView.Zones;
                var nxCam = new NxCamera();

                nxCam.Name = cameraView.camName;
                //nxCam.Guid = cameraView.cameraGuid;
                //nxCam.NxSystem = db.NxSystems.First(sys => sys.Name == cameraView.sysName);
                nxCam.TT = db.TTs.First(t => t.Name == cameraView.ttName);

                if (zones.Count() > 0)
                {
                    // Обновляем информацию о камере в БД
                    switch (zones.First().VmsType)
                    {
                        case VmsTypes.NxWitness:
                            var nxCameras = db.NxCameras.Include(t => t.TT).Where(cam => cam.Guid == cameraView.cameraGuid);
                            if (nxCameras.Count() == 0)
                            {
                                db.NxCameras.Add(nxCam);
                            }
                            else
                            {
                                var camToUpdate = nxCameras.First();
                                camToUpdate.TT = nxCam.TT;
                                db.NxCameras.Update(camToUpdate);
                            }
                            break;
                        case VmsTypes.CMS:
                            break;
                    }

                    // Обновляем зоны в БД
                    foreach (var zone in zones)
                    {
                        if (zone.Id > 0)
                        {
                            db.zones.Update(zone);
                        }
                        else
                        {
                            db.zones.Add(zone);
                        }
                    }

                    db.SaveChanges();
                }

                result.Ok = true;
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                
                result.Ok = false;
                result.Title = "Ошибка сохранения зоны: ApiController -> SaveZone";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return new ObjectResult(result);
            }
        }

        // Удаление зоны
        [HttpGet("DeleteZone/{Id}")]
        public IActionResult DeleteZone(string Id)
        {
            var result = new Result<string>();

            try
            {
                var zone = db.zones.First(z => z.Id == int.Parse(Id));
                db.zones.Remove(zone);
                db.SaveChanges();

                result.Ok = true;
                return new ObjectResult(result);
            }

            catch (Exception e)
            {                
                result.Ok = false;
                result.Title = "Ошибка удаления зоны в SettingsController -> DeleteZone";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                ModelState.AddModelError("", result.ErrorMessage);
                return PartialView("Error", result);
            }
        }

        // Сравнение картинок
        [HttpGet("Compare")]
        public IActionResult Compare(string jsn, string dat)
        {            
            jsn = jsn.Replace("pp", " ");
            var zone = JsonConvert.DeserializeObject<Zone>(jsn);

            byte[] sourceBytes;
            byte[] secondBytes;

            //var cam = db.NxCameras.Include(s => s.NxSystem).First(c => c.Guid == zone.CameraGuid);
            //sourceBytes = cam.SourceImage;           

            if (dat == "Исходное изображение для сравнения")
            {
                //secondBytes = sourceBytes;
            }
            else
            {
                dat = dat.Substring(dat.Length - 16, 16);
                var dateTime = new DateTime();
                try
                {
                    dateTime = DateTime.ParseExact(dat, "dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    dateTime = DateTime.Now;
                }
                try
                {
                    var nx = new module_NX.NX();
                    //var secondImage = nx.GetCameraPicture(dateTime, cam, 700).Data;

                    using (var ms = new MemoryStream())
                    {
                        //secondImage.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        secondBytes = ms.ToArray();
                    }
                }
                catch
                {
                    return new ObjectResult("can't load");
                }
            }
            //if (sourceBytes.Length > 0 & secondBytes.Length > 0 & zone.PolyPoints.Length > 0 & int.Parse(zone.Level) > 0)
            //{
                //try
                //{
                    //var AIShowcase = new module_AIShowcase.Compare2Image(sourceBytes, secondBytes, zone.PolyPoints, int.Parse(zone.Level));
                    //return new ObjectResult(AIShowcase.Value + "%");
                    //return new ObjectResult("debug");
                //}
                //catch
                //{
                    //return new ObjectResult("error");
                //}
            //}
            //else
            //{
                //return new ObjectResult("not correct parameters");
            //}
            return null;
        }

        // Получение ссылки на изображение из БД
        [HttpGet("GetSourceImage")]
        public IActionResult GetSourceImage(string camGuid)
        {
            var nxcam = db.NxSystems.First(c => c.Guid == camGuid);

            //return File(nxcam.SourceImage, "image/jpeg");
            return null;
        }
        
        // получение времени сессии из БД
        [HttpGet("GetSessionTime")]
        public IActionResult GetSessionTime()
        {
            // Получаем id пользователя из файловой БД
            var idFromSql = db.Users.FirstOrDefault(x => x.Name == User.Identity.Name).Id;
            var sessionStartFromDBSQL = dbSql.UserSessions.FirstOrDefault(x => x.Id == idFromSql);
            var sessionTime = db.PortalSettings.FirstOrDefault().SessionTime;

            // Вычисляем конец сессии
            var sessionEndTime = sessionStartFromDBSQL.Date.AddMinutes(sessionTime);

            var timeLeft = sessionEndTime - DateTime.Now;

            double minutesLeft = timeLeft.TotalMinutes;

            return new ObjectResult(minutesLeft);
        }

    }
}
