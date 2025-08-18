using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RKNet_Model;
using Portal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.ViewModels;
using System.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Text;
using Portal.Models.MSSQL;

namespace Portal.Controllers
{
    [Authorize(Roles = "settings,TTSettings")]
    public class SettingsController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;

        IHostedService aishowcaseService = null;
       // IHostedService photocamService = null;
        IHostedService cashMessages = null;
        IHostedService skuStopService = null;

        public SettingsController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext, IEnumerable<IHostedService> hostedServices)
        {
            dbSql = dbSqlContext;
            db = context;
            aishowcaseService = hostedServices.Where(c => c.GetType().Name == nameof(HostedServices.AIShocaseService)).FirstOrDefault();
        //    photocamService = hostedServices.Where(c => c.GetType().Name == nameof(HostedServices.PhotoCamService)).FirstOrDefault();
            cashMessages = hostedServices.Where(c => c.GetType().Name == nameof(HostedServices.CashMessagesService)).FirstOrDefault();
            skuStopService = hostedServices.Where(c => c.GetType().Name == nameof(HostedServices.SkuStopService)).FirstOrDefault();
        }
        

        // Общие настройки
        public IActionResult Main()
        {
            var portalSettings = db.PortalSettings.FirstOrDefault();
            return PartialView(portalSettings);
        }

        // сохранение общих настроек
        public IActionResult SaveSettings(string json)
        {
            try
            {
                json = json.Replace("pp", "+");
                var newSett = JsonConvert.DeserializeObject<PortalSettings>(json);
                var settings = db.PortalSettings.FirstOrDefault();
                settings.SessionTime = newSett.SessionTime;

                db.PortalSettings.Update(settings);
                db.SaveChanges();

                return new ObjectResult("настройки успешно сохранены");
            }
            catch(Exception e)
            {
                return new ObjectResult(e.ToString());
            }            
        }

        //public async Task<IActionResult> UpdateSaleObjects(string daysAgo)
        //{
        //    try
        //    {
        //        DateTime today = DateTime.Now;
        //        string morning = new DateTime(today.Year, today.Month, today.Day, 3, 0, 0).ToString("yyyy-dd-MM HH:mm");
        //        string connectionString = dbSql.Database.GetDbConnection().ConnectionString;

        //        if (dbSql.SettingsVariables.FirstOrDefault(x => x.Name == "UpdateSaleobjectsActive").Value == 1)
        //        {
        //            return BadRequest("Запрос уже выполняется другим пользователем, попробуйте позже");
        //        }

        //        using (var connection = new SqlConnection(connectionString))
        //        {
        //            await connection.OpenAsync();
        //            using (var command = new SqlCommand($"EXEC RKNET.dbo.UpdateSaleobjects @now = '{morning}', @daysAgo = '-{daysAgo}' ", (SqlConnection)connection))
        //            {
        //                command.CommandTimeout = 1500;
        //                int rowsAffected = await command.ExecuteNonQueryAsync();
        //                return Ok(rowsAffected);
        //            }
        //        }
        //    }
        //    catch (SqlException sqlEx)
        //    {
        //        return StatusCode(500, $"SQL Error: {sqlEx.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        public IActionResult UpdateSaleObjects(string daysAgo)
        {
            try
            {
                if (dbSql.SettingsVariables.FirstOrDefault(x => x.Name == "UpdateSaleobjectsActive").Value == 1)
                {
                    return Ok("Запрос уже выполняется другим пользователем, попробуйте позже");
                }

                Task.Run(() => ExecuteUpdateSaleObjectsAsync(daysAgo));

                return Ok("Запрос на обновление данных отправлен");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        private async Task<IActionResult> ExecuteUpdateSaleObjectsAsync(string daysAgo)
        {
            try
            {
                DateTime today = DateTime.Now;
                string morning = new DateTime(today.Year, today.Month, today.Day, 3, 0, 0).ToString("yyyy-dd-MM HH:mm");
                string connectionString = "Data Source=RKSQL.shzhleb.ru\\SQL2019; Initial Catalog=RKNET; User ID=rk7; Password=wZSbs6NKl2SF";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand($"EXEC RKNET.dbo.UpdateSaleobjects @now = '{morning}', @daysAgo = '-{daysAgo}' ", (SqlConnection)connection))
                    {
                        command.CommandTimeout = 900;
                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка: {ex.Message}");
            }
        }

        public IActionResult CheckStatusFromUpdateSaleObjects()
        {
            SettingsVariables settingsVariables = dbSql.SettingsVariables.FirstOrDefault();

            if(settingsVariables.Value == 0 && settingsVariables.Error != null)
            {
                return BadRequest($"Ошибка: {settingsVariables.Error}");
            }

            if(settingsVariables.Value == 1)
            {
                return StatusCode(100, "В процессе выполнения");
            }

            return Ok();
        }

        // Р-КИПЕР------------------------------------------------------------------------------------------
        // горизонтальное меню
        public IActionResult Rk()
        {
            return PartialView();
        }
        // настройки р-кипер
        public IActionResult Rk_Settings()
        {
            var rkSettings = new RKNet_Model.RKSettings();
            if (db.RKSettings.FirstOrDefault() != null)
                rkSettings = db.RKSettings.FirstOrDefault();

            return PartialView(rkSettings);
        }

        // сохранение настроек Р-Кипер
        public IActionResult RkSave(string json)
        {
            try
            {
                json = json.Replace("pp", "+");
                var newSett = JsonConvert.DeserializeObject<RKNet_Model.RKSettings>(json);

                // проверяем корректность ввода
                System.Net.IPAddress ip;
                var correctIp = System.Net.IPAddress.TryParse(newSett.RefServerIp, out ip);
                if(!correctIp)
                {
                    return new ObjectResult("некорректный ip адресс сервера справочников");
                }
                else
                {
                    newSett.RefServerIp = ip.ToString();
                }

                int refPort;
                if(!int.TryParse(newSett.RefServerPort, out refPort))
                {
                    return new ObjectResult("некорректный порт сервера справочников");
                }

                int cashPort;
                if (!int.TryParse(newSett.CashPort, out cashPort))
                {
                    return new ObjectResult("некорректный порт кассовой станции");
                }

                if(newSett.User.Length == 0)
                {
                    return new ObjectResult("не заполнено имя пользователя");
                }

                if (newSett.Password.Length == 0)
                {
                    return new ObjectResult("не заполнен пароль");
                }

                // сохраняем настройки бд
                var rkSettings = db.RKSettings.FirstOrDefault();
                if(rkSettings != null)
                {
                    rkSettings.RefServerIp = newSett.RefServerIp;
                    rkSettings.RefServerPort = newSett.RefServerPort;
                    rkSettings.CashPort = newSett.CashPort;
                    rkSettings.User = newSett.User;
                    rkSettings.Password = newSett.Password;

                    db.RKSettings.Update(rkSettings);
                }
                else
                {
                    rkSettings = new RKSettings();

                    rkSettings.RefServerIp = newSett.RefServerIp;
                    rkSettings.RefServerPort = newSett.RefServerPort;
                    rkSettings.CashPort = newSett.CashPort;
                    rkSettings.User = newSett.User;
                    rkSettings.Password = newSett.Password;

                    db.RKSettings.Add(rkSettings);
                }
                
                db.SaveChanges();

                return new ObjectResult("настройки успешно сохранены");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // запросы
        public IActionResult Rk_Requests()
        {
            var reqView = new ViewModels.Settings.RkRequestsView();
            reqView.Cashes = db.CashStations.OrderBy(c => c.Name).ToList();
            reqView.RefIp = db.RKSettings.FirstOrDefault().RefServerIp;

            return PartialView(reqView);
        }

        public IActionResult Rk_SendRequest(string json)
        {
            var result = new Result<string>();
            var reqView = JsonConvert.DeserializeObject<ViewModels.Settings.RkRequestsView>(json);

            // проверяем корректность ввода            
            System.Net.IPAddress ip;
            var ipOk = System.Net.IPAddress.TryParse(reqView.ip, out ip);

            if(!ipOk)
            {
                result.Ok = false;
                result.ErrorMessage = "некорректный ip адрес";
                return new ObjectResult(result);
            }

            if (reqView.xml_request.Length == 0)
            {
                result.Ok = false;
                result.ErrorMessage = "введите запрос";
                return new ObjectResult(result);
            }

            // отправка запроса
            var rkSettings = db.RKSettings.FirstOrDefault();
            string port;

            if (reqView.ip == rkSettings.RefServerIp)
                port = rkSettings.RefServerPort;
            else
                port = rkSettings.CashPort;

            var rk = new RKNet_Model.Rk7XML.RK7();
            result = rk.SendRequest(reqView.ip, reqView.xml_request, port, rkSettings.User, rkSettings.Password);

            return new ObjectResult(result);
        }

        // ДОСТУП-------------------------------------------------------------------------------------------
        public IActionResult Access()
        {
            return PartialView();
        }

        // ВИДЕОНАБЛЮДЕНИЕ----------------------------------------------------------------------------------
        
        // горизонтальное меню
        public IActionResult Video()
        {
            return PartialView();         
        }
        // видеосистемы
        public IActionResult Video_Systems()
        {
            try
            {
                var systems = db.NxSystems.ToList();
                var nxRequest = new module_NX.NX();

                // Получаем количество камер, серверов и статус
                foreach (var nxSystem in systems)
                {
                    var result = nxRequest.GetFullInfo(nxSystem);
                    if (result.Ok)
                    {
                        nxSystem.isOnline = true;
                        nxSystem.CamerasTotal = result.Data.cameraUserAttributesList.Count().ToString();
                        nxSystem.ServersTotal = result.Data.servers.Count().ToString();
                    }
                    else
                    {
                        nxSystem.isOnline = false;
                    }

                }
                return PartialView(systems);
            }

            catch (Exception e)
            {
                var result = new Result<string>();
                result.Title = "Ошибка запроса списка подключенных видеосистем из базы данных";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return PartialView("Error", result);
            }


        }        

        // добавление NX системы - форма
        public IActionResult Video_AddNxSystem()
        {
            var nxSystem = new RKNet_Model.VMS.NX.NxSystem();
            nxSystem.Name = "empty";
            return PartialView(nxSystem);
        }

        // добавление NX системы - запись в БД
        public IActionResult Video_postAddNxSystem(string jsn)
        {
            var result = new Result<module_NX.NX.FullInfo>();
            try
            {
                jsn = jsn.Replace("pp", " ");
                var nxSystem = JsonConvert.DeserializeObject<RKNet_Model.VMS.NX.NxSystem>(jsn);

                var nx = new module_NX.NX();                
                result = nx.GetFullInfo(nxSystem);                

                if (result.Ok)
                {
                    var fullinfo = result.Data;

                    nxSystem.Name =fullinfo.allProperties.FirstOrDefault(p => p.name == "systemName").value;
                    nxSystem.Guid = fullinfo.allProperties.FirstOrDefault(p => p.name == "systemName").resourceId;
                    
                    var nxToModify = db.NxSystems.Include(s => s.NxCameras).FirstOrDefault(n => n.Name == nxSystem.Name);
                    
                    // новая система
                    if (nxToModify == null)
                    {                        
                        db.NxSystems.Add(nxSystem);
                        db.SaveChanges();

                        return RedirectToAction("Video_Systems");
                    }
                    // существующая система
                    else
                    {
                        nxToModify.Host = nxSystem.Host;
                        nxToModify.Port = nxSystem.Port;
                        nxToModify.Login = nxSystem.Login;
                        nxToModify.Password = nxSystem.Password;
                        nxToModify.Description = nxSystem.Description;                        

                        db.NxSystems.Update(nxToModify);
                        db.SaveChanges();

                        return RedirectToAction("Video_Systems");
                    }
                }
                // ошибки модуля
                else
                {
                    if (result.ErrorMessage.Contains("401"))
                    {
                        result.ErrorMessage = "неверное имя пользователя или пароль";
                        ModelState.AddModelError("User", "");
                        ModelState.AddModelError("Password", "");
                        ModelState.AddModelError("", result.ErrorMessage);
                    }
                    if (result.ErrorMessage.Contains("Этот хост неизвестен"))
                    {
                        result.ErrorMessage = "указанный адрес и/или порт недоступны";
                        ModelState.AddModelError("Host", "");
                        ModelState.AddModelError("Port", "");
                        ModelState.AddModelError("", result.ErrorMessage);
                    }

                    return PartialView("Video_AddNxSystem", nxSystem);
                }
            }
            // ошибки контроллера
            catch (Exception e)
            {
                result.Title = "Ошибка добавления NX системы в SettingsController -> postAddNxSystem(string jsn)";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                ModelState.AddModelError("", result.ErrorMessage);
                return PartialView("Video_AddNxSystem");
            }
        }

        // удаление NX системы
        public IActionResult Video_DeleteNxSystem(string jsn)
        {
            var result = new Result<string>();
            jsn = jsn.Replace("pp", " ");
            var nxSystem = JsonConvert.DeserializeObject<RKNet_Model.VMS.NX.NxSystem >(jsn);

            try
            {
                var system = db.NxSystems.FirstOrDefault(nx => nx.Id == nxSystem.Id);

                // удаляем камеры
                db.NxCameras.RemoveRange(system.NxCameras);
                
                // удаляем систему
                db.NxSystems.Remove(system);                               

                db.SaveChanges();
                return RedirectToAction("Video_Systems");
            }

            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // изменение NX системы
        public IActionResult Video_ModifyNxSystem(string jsn)
        {
            var result = new RKNet_Model.Result<string>();
            jsn = jsn.Replace("pp", " ");
            var nxSystem = JsonConvert.DeserializeObject<RKNet_Model.VMS.NX.NxSystem>(jsn);

            try
            {
                var nxToModify = db.NxSystems.First(nx => nx.Id == nxSystem.Id);
                return PartialView("Video_AddNxSystem", nxToModify);
            }

            catch (Exception e)
            {
                result.Title = "Ошибка изменения NX системы в SettingsController -> ModifyNxSystem(string jsn)";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                ModelState.AddModelError("", result.ErrorMessage);
                return PartialView("Error", result);
            }
        }
        
        // расписание снимков
        public IActionResult Video_DefaultShedule(string startTime, string stopTime, string interval)
        {
            try
            {
                // вывод данных из БД                
                if (interval == null)
                {
                    var module = db.Modules.FirstOrDefault(m => m.Name == "PhotoCam");
                    return PartialView(module);
                }
                // запись в БД
                else
                {
                    var module = db.Modules.FirstOrDefault(m => m.Name == "PhotoCam");
                    module.StartTime = startTime;
                    module.StopTime = stopTime;
                    module.Interval = interval;

                    db.SaveChanges();
                    moduleRestart(module.Id);

                    return new ObjectResult("Успешно сохранено");
                }
            }
            catch(Exception e)
            {
                return new ObjectResult(e.ToString());
            }            
        }       

        // ЗАПОЛНЕНИЕ ВИТРИН -------------------------------------------------------------------------------
        public IActionResult Showcase()
        {
            var TTs = new List<TTZone>();

            foreach (var z in db.zones)
            {
                switch (z.VmsType)
                {
                    case RKNet_Model.VMS.VmsTypes.NxWitness:
                        var ttName = db.NxCameras.Include(t => t.TT).Where(c => c.Guid == z.CameraGuid).First().TT.Name;
                        var added = false;
                        foreach (var tt in TTs)
                        {
                            if (tt.TTName == ttName)
                            {
                                tt.Zones.Add(z);
                                added = true;
                                break;
                            }
                        }
                        if (!added)
                        {
                            var ttZone = new TTZone();
                            ttZone.TTName = ttName;
                            ttZone.Zones.Add(z);
                            TTs.Add(ttZone);
                        }
                        break;
                }

            }

            TTs.OrderBy(c => c.TTName);

            return PartialView(TTs);
        }

        // Заполлнение витрин: Шаг 1 - выбрать камеру
        public IActionResult ShowcaseAddCamera01(string jsn)
        {
            CameraView camView;

            if(jsn != null)
            {
                jsn = jsn.Replace("pp", " ");
                camView = JsonConvert.DeserializeObject<ViewModels.CameraView>(jsn);
            }
            else
            {
                camView = new CameraView();
            }
            try
            {
               //camView.NxSystems = db.NxSystems.ToList();
                return PartialView(camView);
            }

            catch (Exception e)
            {
                var result = new RKNet_Model.Result<string>();
                result.Title = "Ошибка получения списка NX систем в SettingsController -> ShowcaseAddCamera";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                ModelState.AddModelError("", result.ErrorMessage);
                return PartialView("Error", result);
            }

        }
        // Заполнененность витрин: Шаг 2 - выбрать образец пустой витрины
        public IActionResult ShowcaseAddCamera02(string jsn)
        {
            try
            {
                jsn = jsn.Replace("pp", " ");
                var camView = JsonConvert.DeserializeObject<CameraView>(jsn);

                return PartialView(camView);
            }
            catch(Exception e)
            {
                var result = new Result<string>();
                result.Title = "Ошибка загрузки интервала снимков выбранной камеры (возможно, не были переданы данные камеры): SettingsController -> ShowcaseAddCamera02";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return PartialView("Error", result);
            }
        }
        // Заполнененность витрин: Шаг 3 - выбрать образец пустой витрины
        public IActionResult ShowcaseAddCamera03(string jsn)
        {            
            try
            {
                jsn = jsn.Replace("pp", " ");
                var camView = JsonConvert.DeserializeObject<CameraView>(jsn);

                if (camView.sysName == null)
                {
                    switch (camView.vmsType)
                    {
                        case RKNet_Model.VMS.VmsTypes.NxWitness:
                            //var nxCameras = db.NxCameras.Include(s => s.NxSystem).Where(c => c.Guid == camView.cameraGuid);
                            //if (nxCameras.Count() > 0)
                            //{
                                //camView.sysName = nxCameras.First().NxSystem.Name;
                                //camView.camName = nxCameras.First().Name;
                                //camView.dateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                            //}

                            break;
                    }
                }
                else
                {
                    // Получаем исходную картинку для сравнения и пишем её в таблицу БД камеры (временно, потом нужно будет привязать к зоне)

                    var dateTime = new DateTime();
                    try
                    {
                        dateTime = DateTime.ParseExact(camView.dateTime, "dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        dateTime = DateTime.Now;
                    }

                    var nxSystem = db.NxSystems.First(c => c.Name == camView.sysName);
                    var nx = new module_NX.NX();

                    //var nxCam = new RKNet_Model.VMS.NX.NxSystem.NxCamera();
                    //nxCam.NxSystem = nxSystem;
                    //nxCam.Guid = camView.cameraGuid;

                    //var GetCamPicture = nx.GetCameraPicture(dateTime, nxCam, 700);
                    byte[] imageData;
                    using (var ms = new System.IO.MemoryStream())
                    {
                        //GetCamPicture.Data.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

                        imageData = ms.ToArray();
                    }
                    // Если камеры еще нет в БД, то добавляем её
                    //var camsWithGuid = db.NxSystems.Where(c => c.Guid == nxCam.Guid);
                    //if (camsWithGuid.Count() > 0)
                    //{
                        //var camToSave = db.nx_cameras.First(c => c.Guid == nxCam.Guid);
                        //camToSave.SourceImage = imageData;
                        //db.nx_cameras.Update(camToSave);
                    //}
                    //else
                    //{
                        //nxCam.Name = camView.camName;
                        //nxCam.SourceImage = imageData;
                        //db.nx_cameras.Add(nxCam);
                    //}
                    //db.SaveChanges();
                }

                return PartialView(camView);              
            }
            catch (Exception e)
            {
                var result = new Result<string>();
                result.Title = "Ошибка загрузки области построения зон выбранной камеры (возможно, не были переданы данные камеры): SettingsController -> ShowcaseAddCamera03";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return PartialView("Error", result);
            }
        }
        // Создание новой зоны
        public IActionResult ShowcaseAddCamera03_zoneAdd(string Id, string Guid)
        {
            try
            {
                var zoneView = new ZoneView();
                
                zoneView.TTs = db.TTs.OrderBy(n => n.Name).ToList();

                if (Id != "add")
                {
                    zoneView.Zone = db.zones.First(z => z.Id == int.Parse(Id));
                }

                switch (zoneView.Zone.VmsType)
                {
                    case RKNet_Model.VMS.VmsTypes.NxWitness:
                        var nxCameras = db.NxCameras.Include(t => t.TT).Where(c => c.Guid == Guid).Where(c => c.TT != null);
                        if (nxCameras.Count() > 0)
                        {
                            zoneView.ttName = nxCameras.First().TT.Name;
                        }
                        break;
                    case RKNet_Model.VMS.VmsTypes.CMS:
                        break;
                }

                return PartialView(zoneView);
            }
            catch(Exception e)
            {
                var result = new Result<string>();
                result.Title = "Ошибка загрузки данных зоны: SettingsController -> ShowcaseAddCamera03_zoneAdd";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return PartialView("Error", result);
            }
        }

        public IActionResult ShowcaseAddCamera03_zoneList(string jsn)
        {
            try
            {
                jsn = jsn.Replace("pp", " ");
                var camView = JsonConvert.DeserializeObject<CameraView>(jsn);
                camView.Zones = db.zones.Where(z => z.CameraGuid == camView.cameraGuid).ToList();
                
                return PartialView(camView);
            }
            catch (Exception e)
            {
                var result = new Result<string>();
                result.Title = "Ошибка загрузки области построения зон выбранной камеры (возможно, не были переданы данные камеры): SettingsController -> ShowcaseAddCamera03_zoneList";
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return PartialView("Error", result);
            }
        }

        // МОДУЛИ -------------------------------------------------------------------------------------------
        public IActionResult Modules()
        {
            var modules = db.Modules.ToList();
            return PartialView(modules);
        }

        public IActionResult ModuleChange(int id, bool enabled)
        {
            var result = new Result<string>();
            
            try
            {
                var module = db.Modules.FirstOrDefault(m => m.Id == id);
                module.Enabled = enabled;
                db.SaveChanges();

                switch(module.Id)
                {
                    case 1:
                        if(module.Enabled)
                            aishowcaseService.StartAsync(new System.Threading.CancellationToken());
                        else
                            aishowcaseService.StopAsync(new System.Threading.CancellationToken());
                        break;
                    //case 2:
                    //    if (module.Enabled)
                    //        photocamService.StartAsync(new System.Threading.CancellationToken());
                    //    else
                    //        photocamService.StopAsync(new System.Threading.CancellationToken());
                    //    break;
                    case 4:
                        if (module.Enabled)
                            cashMessages.StartAsync(new System.Threading.CancellationToken());
                        else
                            cashMessages.StopAsync(new System.Threading.CancellationToken());
                        break;
                    case 5:
                        if (module.Enabled)
                            skuStopService.StartAsync(new System.Threading.CancellationToken());
                        else
                            skuStopService.StopAsync(new System.Threading.CancellationToken());
                        break;
                    default:
                        break;
                }

                // логгируем
                var log = new Models.LogEvent<string>(User);
                if (enabled)
                {
                    log.Name = "Включение модуля";                    
                }
                else
                {
                    log.Name = "Выключение модуля";
                }
                log.Description = module.Description;
                log.IpAdress = HttpContext.Session.GetString("ip");
                log.Save();

            }
            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
            }            

            return new ObjectResult(result);
        }        

        public void moduleRestart(int moduleId)
        {
            var module = db.Modules.FirstOrDefault(m => m.Id == moduleId);

            switch (module.Id)
            {
                case 1:
                    if (module.Enabled)
                    {
                        aishowcaseService.StopAsync(new System.Threading.CancellationToken());
                        aishowcaseService.StartAsync(new System.Threading.CancellationToken());
                    }
                    break;
                //case 2:
                //    if (module.Enabled)
                //    {
                //        photocamService.StopAsync(new System.Threading.CancellationToken());
                //        photocamService.StartAsync(new System.Threading.CancellationToken());
                //    }
                //    break;
                case 4:
                    if (module.Enabled)
                    {
                        cashMessages.StopAsync(new System.Threading.CancellationToken());
                        cashMessages.StartAsync(new System.Threading.CancellationToken());
                    }
                    break;
                case 5:
                    if (module.Enabled)
                    {
                        skuStopService.StopAsync(new System.Threading.CancellationToken());
                        skuStopService.StartAsync(new System.Threading.CancellationToken());
                    }
                    break;
                default:
                    break;
            }
        }
    }


}
