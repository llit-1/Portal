using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models.MSSQL.Location;
using Portal.Models.MSSQL.Personality;
using Portal.ViewModels.Settings_TT;
using RKNet_Model;
using RKNet_Model.Account;
using RKNet_Model.CashClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;

namespace Portal.Controllers
{
    [Authorize(Roles = "settings")]
    public class Settings_TTController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        public Settings_TTController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }


        // Горизонтальное меню        
        public IActionResult TabMenu()
        {
            return PartialView();
        }

        // Торговые Точки ***********************************************************
        // Шапка + разметка для вывода
        public IActionResult TTs()
        {
            return PartialView();
        }

        // Таблица ТТ
        public IActionResult TTsTable(bool closedTT, bool ecTT)
        {
            if (db.TTs is null)
            {
                return Ok();
            }
            var tts = db.TTs
                .Include(t => t.Users)
                .Include(t => t.CashStations)
                .Include(t => t.NxCameras)
                .Include(t => t.Type)
                .Include(t => t.Organization)
                .Where(t => !t.Closed)
                .ToList();
            if (closedTT==false) { tts.RemoveAll(item => item.CloseDate != null); }
            if (ecTT==false) { tts.RemoveAll(item => item.Type?.Name == "УЦ"); }
            return PartialView(tts);
        }

        // Редактор ТТ
        public IActionResult TTEdit(int ttId)
        {
            var ttSettings = new TTSettings();

            ttSettings.TT = db.TTs
                .Include(t => t.Users)
                .Include(t => t.CashStations)
                .Include(t => t.NxCameras)
                .FirstOrDefault(t => t.Id == ttId);

            if (ttSettings.TT == null)
            {
                ttSettings.TT = new RKNet_Model.TT.TT();
                ttSettings.newTT = true;
            }

            ttSettings.TTtypes = db.TTtypes.ToList();
            ttSettings.Users = db.Users.ToList();
            ttSettings.Organizations = db.Organizations.ToList();
            return PartialView(ttSettings);
        }

        // Сохранение ТТ
        public IActionResult TTSave(string ttjsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                ttjsn = ttjsn.Replace("%bkspc%", " ");
                var ttJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.tt>(ttjsn);

                // существующая тт
                if (ttJsn.id != 0)
                {
                    var tt = db.TTs
                        .Include(t => t.Users)
                        .Include(t => t.CashStations)
                        .Include(t => t.NxCameras)
                        .FirstOrDefault(t => t.Id == ttJsn.id);

                    switch (ttJsn.attribute)
                    {
                        case "ttName":
                            if (!string.IsNullOrEmpty(ttJsn.name))
                            {
                                tt.Name = ttJsn.name;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Название торговой точки заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existName = db.TTs.FirstOrDefault(t => t.Name == tt.Name);
                            if (existName != null)
                            {
                                result.Ok = false;
                                result.Data = "Торговая точка с названием \"" + existName.Name + "\" уже существует, введите другое имя.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "ttRestaurantSifr":
                            tt.Restaurant_Sifr = ttJsn.restaurant_Sifr;
                            break;

                        case "ttAddress":
                            if (!string.IsNullOrEmpty(ttJsn.address))
                            {
                                tt.Address = ttJsn.address;
                            }
                            else
                            {
                                tt.Address = null;
                            }
                            break;

                        case "ttCode":
                            if (!string.IsNullOrEmpty(ttJsn.code))
                            {
                                tt.Code = int.Parse(ttJsn.code);
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Код торговой точки заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existCode = db.TTs.FirstOrDefault(t => t.Code == tt.Code);
                            if (existCode != null)
                            {
                                result.Ok = false;
                                result.Data = "Торговая точка с кодом \"" + existCode.Code + "\" уже существует, введите другой код.";
                                return new ObjectResult(result);
                            }

                            break;

                        case "ttObd":
                            if (!string.IsNullOrEmpty(ttJsn.obd))
                            {
                                tt.Obd = int.Parse(ttJsn.obd);
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Код ОБД заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existObd = db.TTs.FirstOrDefault(t => t.Obd == tt.Obd);
                            if (existObd != null)
                            {
                                result.Ok = false;
                                result.Data = "Код ОБД \"" + existObd.Obd + "\" уже существует, введите другой код.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "openDate":

                            DateTime open;
                            var Ok = DateTime.TryParse(ttJsn.openDate, out open);

                            if (Ok)
                            {
                                tt.OpenDate = open.Date;
                            }
                            else
                            {
                                tt.OpenDate = null;
                            }

                            break;

                        case "closeDate":

                            DateTime close;
                            Ok = DateTime.TryParse(ttJsn.closeDate, out close);

                            if (Ok)
                            {
                                tt.CloseDate = close.Date;
                            }
                            else
                            {
                                tt.CloseDate = null;
                            }

                            break;

                        case "ttType":

                            foreach (var item in ttJsn.items)
                            {
                                tt.Type = db.TTtypes.FirstOrDefault(t => t.Id == item.id);
                            }

                            break;

                        case "ttOrganization":

                            foreach (var item in ttJsn.items)
                            {
                                tt.Organization = db.Organizations.FirstOrDefault(o => o.Id == item.id);
                            }
                            break;

                        case "users":
                            tt.Users = new List<User>();
                            foreach (var item in ttJsn.items)
                            {
                                var user = db.Users.FirstOrDefault(u => u.Id == item.id);
                                tt.Users.Add(user);
                            }
                            break;

                        case "yandexEda":
                            tt.YandexEda = ttJsn.yandexEda;
                            break;

                        case "deliveryClub":
                            tt.DeliveryClub = ttJsn.deliveryClub;
                            break;

                        case "cashes":
                            var a = tt.CashStations;
                            // обновляем кассы
                            // удаляем кассы из БД, которые были откреплены от ТТ
                            var cashesDelete = tt.CashStations.Where(c => !ttJsn.cashes.Select(i => i.Id).Contains(c.Id)).ToList();
                            db.CashStations.RemoveRange(cashesDelete);


                            foreach (var item in ttJsn.cashes)
                            {
                                var cash = new RKNet_Model.Rk7XML.CashStation();

                                IPAddress ip;
                                var correctIp = IPAddress.TryParse(item.Ip, out ip);
                                if (!correctIp)
                                {
                                    result.Ok = false;
                                    result.Data = "для кассы " + item.Name + " указан некорректный ip адрес";
                                    return new ObjectResult(result);
                                }

                                if (string.IsNullOrEmpty(item.Name))
                                {
                                    result.Ok = false;
                                    result.Data = "Имя кассы не можеть быть пустым";
                                    return new ObjectResult(result);
                                }

                                // новая касса
                                if (item.Id == 0)
                                {
                                    // проверяем есть ли уже касса с таким ip в бд
                                    var cashExist = db.CashStations.Where(c => c.Ip == ip.ToString()).Count();
                                    if (cashExist > 0)
                                    {
                                        var existCash = db.CashStations.Include(t => t.TT).FirstOrDefault(c => c.Ip == ip.ToString());
                                        result.Ok = false;
                                        result.Data = "Касса с данным ip-адресом уже привязана к точке " + existCash.TT.Name + ", изменения не будут сохранены.";
                                        return new ObjectResult(result);
                                    }

                                    // добавляем кассу в бд
                                    cash.Name = item.Name;
                                    cash.Ip = ip.ToString();
                                    cash.TT = tt;
                                    cash.Midserver = item.Midserver;
                                    db.CashStations.Add(cash);
                                }
                                // существующая касса
                                else
                                {
                                    cash = db.CashStations.FirstOrDefault(c => c.Id == item.Id);
                                    cash.Name = item.Name;
                                    cash.Midserver = item.Midserver;
                                    cash.Ip = ip.ToString();
                                    db.CashStations.Update(cash);
                                }
                            }
                            break;
                            /*
                            case "cameras":
                                tt.NxCameras = new List<RKNet_Model.VMS.NX.NxCamera>();
                                foreach (var item in ttJsn.items)
                                {
                                    var camera = db.NxCameras.FirstOrDefault(c => c.Id == item.id);
                                    tt.NxCameras.Add(camera);
                                }
                                break;
                            */
                    }

                    db.TTs.Update(tt);
                    db.SaveChanges();
                }
                // новая ТТ
                else
                {
                    var tt = new RKNet_Model.TT.TT();
                    var ttUsers = new List<User>();
                    var ttCameras = new List<RKNet_Model.VMS.NX.NxCamera>();
                    var ttCashes = new List<RKNet_Model.Rk7XML.CashStation>();

                    foreach (var item in ttJsn.users)
                    {
                        ttUsers.Add(db.Users.First(u => u.Id == item.id));
                    }

                    // тип новой тт
                    if (!string.IsNullOrEmpty(ttJsn.type))
                    {
                        tt.Type = db.TTtypes.FirstOrDefault(t => t.Id == int.Parse(ttJsn.type));
                    }

                    // организация новой тт
                    if (!string.IsNullOrEmpty(ttJsn.organization))
                    {
                        tt.Organization = db.Organizations.FirstOrDefault(o => o.Id == int.Parse(ttJsn.organization));
                    }

                    // имя тт
                    if (!string.IsNullOrEmpty(ttJsn.name))
                    {
                        tt.Name = ttJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название тороговой точки заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existName = db.TTs.FirstOrDefault(t => t.Name == tt.Name);
                    if (existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Торговая точка с именем \"" + existName.Name + "\" уже существует, введите другое имя.";
                        return new ObjectResult(result);
                    }

                    // код RK
                    tt.Restaurant_Sifr = ttJsn.restaurant_Sifr;


                    // Адрес ТТ
                    if (!string.IsNullOrEmpty(ttJsn.address))
                    {
                        tt.Address = ttJsn.address;
                    }
                    else
                    {
                        tt.Address = null;
                    }

                    // код ТТ
                    if (!string.IsNullOrEmpty(ttJsn.code))
                    {
                        tt.Code = int.Parse(ttJsn.code);
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Код торговой точки заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existCode = db.TTs.FirstOrDefault(t => t.Code == tt.Code);
                    if (existCode != null)
                    {
                        result.Ok = false;
                        result.Data = "Торговая точка с кодом \"" + existCode.Code + "\" уже существует, введите другой код.";
                        return new ObjectResult(result);
                    }

                    // Код ОБД
                    if (!string.IsNullOrEmpty(ttJsn.obd))
                    {
                        tt.Obd = int.Parse(ttJsn.obd);
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Код ОБД заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existObd = db.TTs.FirstOrDefault(t => t.Obd == tt.Obd);
                    if (existObd != null)
                    {
                        result.Ok = false;
                        result.Data = "Код ОБД \"" + existObd.Obd + "\" уже существует, введите другой код.";
                        return new ObjectResult(result);
                    }

                    // Дата открытия
                    DateTime open;
                    var Ok = DateTime.TryParse(ttJsn.openDate, out open);

                    if (Ok)
                    {
                        tt.OpenDate = open.Date;
                    }
                    else
                    {
                        tt.OpenDate = null;
                    }

                    // Дата закрытия
                    DateTime close;
                    Ok = DateTime.TryParse(ttJsn.closeDate, out close);

                    if (Ok)
                    {
                        tt.CloseDate = close.Date;
                    }
                    else
                    {
                        tt.CloseDate = null;
                    }

                    // интеграции
                    tt.YandexEda = ttJsn.yandexEda;
                    tt.DeliveryClub = ttJsn.deliveryClub;


                    // Кассы на новой ТТ
                    foreach (var item in ttJsn.cashes)
                    {
                        var cash = new RKNet_Model.Rk7XML.CashStation();

                        IPAddress ip;
                        var correctIp = IPAddress.TryParse(item.Ip, out ip);
                        if (!correctIp)
                        {
                            result.Ok = false;
                            result.Data = "для кассы " + item.Name + " указан некорректный ip адрес";
                            return new ObjectResult(result);
                        }

                        if (string.IsNullOrEmpty(item.Name))
                        {
                            result.Ok = false;
                            result.Data = "Имя кассы не можеть быть пустым";
                            return new ObjectResult(result);
                        }

                        // новая касса
                        if (item.Id == 0)
                        {
                            // проверяем есть ли уже касса с таким ip в бд
                            var cashExist = db.CashStations.Where(c => c.Ip == ip.ToString()).Count();
                            if (cashExist > 0)
                            {
                                var existCash = db.CashStations.Include(t => t.TT).FirstOrDefault(c => c.Ip == ip.ToString());
                                result.Ok = false;
                                result.Data = "Касса с данным ip-адресом уже привязана к точке " + existCash.TT.Name + ", изменения не будут сохранены.";
                                return new ObjectResult(result);
                            }

                            // добавляем кассу в бд
                            cash.Name = item.Name;
                            cash.Ip = ip.ToString();
                            cash.Midserver = item.Midserver;
                            db.CashStations.Add(cash);
                        }
                    }

                    // камеры на новой тт
                    ttCameras = new List<RKNet_Model.VMS.NX.NxCamera>();
                    foreach (var cam in ttJsn.cameras)
                    {
                        var nxCamera = new RKNet_Model.VMS.NX.NxCamera();
                        nxCamera.Name = cam.name;
                        nxCamera.Guid = cam.id;
                        nxCamera.NxSystem = db.NxSystems.FirstOrDefault(s => s.Id == cam.systemId);
                        ttCameras.Add(nxCamera);
                    }


                    tt.Users = ttUsers;
                    tt.NxCameras = ttCameras;

                    db.TTs.Add(tt);
                    db.SaveChanges();

                    // добавляем сохраненные в БД кассы на ТТ                
                    var ttt = db.TTs.FirstOrDefault(t => t.Code == int.Parse(ttJsn.code));
                    foreach (var item in ttJsn.cashes)
                    {
                        var cash = db.CashStations.FirstOrDefault(c => c.Ip == item.Ip);
                        ttt.CashStations.Add(cash);
                    }

                    Location location = new();
                    LocationVersions locationVersions = new();

                    location.Name = ttJsn.name;
                    location.LocationType = null;
                    location.RKCode = ttJsn.restaurant_Sifr;
                    location.AggregatorsCode = int.Parse(ttJsn.code);
                    dbSql.Add(location);
                    dbSql.SaveChanges();

                    locationVersions.Name = ttJsn.name;
                    locationVersions.Location = location;
                    locationVersions.OBD = int.Parse(ttJsn.obd);
                    locationVersions.Entity = null;
                    locationVersions.Actual = 1;
                    dbSql.Add(locationVersions);
                    dbSql.SaveChanges();

                    db.TTs.Update(ttt);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }

        // Удаление ТТ
        public IActionResult TTDelete(int ttId)
        {
            try
            {
                var tt = db.TTs
                    .Include(t => t.Users)
                    .Include(t => t.CashStations)
                    .Include(t => t.NxCameras)
                    .Include(t => t.Type)
                    .Include(t => t.Organization)
                    .FirstOrDefault(t => t.Id == ttId);

                if (tt.CashStations != null)
                    db.CashStations.RemoveRange(tt.CashStations);
                if (tt.NxCameras != null)
                    db.NxCameras.RemoveRange(tt.NxCameras);

                db.TTs.Remove(tt);
                db.SaveChanges();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Выбранные элементы коллекций на ТТ
        public IActionResult GetTTItems(int ttId, string selectId)
        {
            var tt = db.TTs
                .Include(t => t.Users)
                .Include(t => t.CashStations)
                .Include(t => t.NxCameras)
                .Include(t => t.Organization)
                .Include(t => t.Type)
                .FirstOrDefault(t => t.Id == ttId);

            switch (selectId)
            {
                case "users":
                    return new ObjectResult(tt.Users);
                case "cashes":
                    return new ObjectResult(tt.CashStations);
                case "cameras":
                    return new ObjectResult(tt.NxCameras);
                case "ttType":
                    return new ObjectResult(tt.Type);
                case "ttOrganization":
                    return new ObjectResult(tt.Organization);
                default:
                    return new ObjectResult("empty");
            }
        }

        // Id ТТ по имени
        public IActionResult GetTTId(string ttName)
        {
            var ttId = db.TTs.FirstOrDefault(t => t.Name == ttName).Id;
            return new ObjectResult(ttId);
        }

        // Запрос имени кассы Р-Кипер
        public IActionResult GetCashName(string cashIp)
        {
            var requestResult = new RKNet_Model.Result<string>();
            try
            {
                // проверяем корректность ввода
                if (cashIp == null)
                {
                    requestResult.Ok = false;
                    requestResult.ErrorMessage = "не вевведён ip-адрес кассы";
                    return new ObjectResult(requestResult);
                }

                IPAddress ip;
                var ipCorrect = IPAddress.TryParse(cashIp, out ip);
                if (!ipCorrect)
                {
                    requestResult.Ok = false;
                    requestResult.ErrorMessage = "некорректный ip-адрес кассы";
                    return new ObjectResult(requestResult);
                }

                // отправляем запрос на кассу Р-Кипер
                var xml_request = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><RK7Query><RK7CMD CMD=\"GetSystemInfo2\"></RK7CMD></RK7Query>";

                var rk = new RKNet_Model.Rk7XML.RK7();
                var rkSettings = db.RKSettings.FirstOrDefault();
                var xml_result = rk.SendRequest(ip.ToString(), xml_request, rkSettings.CashPort, rkSettings.User, rkSettings.Password);


                if (xml_result.Ok)
                {
                    var systemInfo2 = RKNet_Model.Rk7XML.Response.GetSystemInfo2Response.RK7QueryResult.DeSerializeQueryResult(xml_result.Data).systemInfo;
                    requestResult.Data = $"{{\"Id\" : \"{systemInfo2.restaurant.id}\",\"Name\" : \"Касса, {systemInfo2.restaurant.name}\",\"Midserver\" : \"{systemInfo2.cashGroup.id}\"}}";
                }
                else
                {
                    requestResult.Ok = false;
                    requestResult.ErrorMessage = xml_result.ErrorMessage;
                }

            }
            catch (Exception e)
            {
                requestResult.Ok = false;
                requestResult.ErrorMessage = e.Message;
                requestResult.ExceptionText = e.ToString();
            }

            return new ObjectResult(requestResult);
        }

        // Свободный код точки для новой ТТ
        public IActionResult NewTTCode()
        {
            var newTTCode = 0;
            var lastTT = db.TTs.OrderBy(t => t.Code).LastOrDefault();

            if (lastTT != null)
            {
                newTTCode = lastTT.Code + 1;
            }

            return new ObjectResult(newTTCode);
        }

        // Организации ***********************************************************
        // Шапка + разметка для вывода
        public IActionResult Organizations()
        {
            return PartialView();
        }

        // Таблица организаций
        public IActionResult OrganizationsTable()
        {
            var organizations = db.Organizations
                .Include(o => o.TTs.Where(t => t.CloseDate == null))
                .ToList();
            return PartialView(organizations);
        }

        // Редактор организаций
        public IActionResult OrganizationEdit(int Id)
        {
            var orgSettings = new OrganizationSettings();

            orgSettings.Organization = db.Organizations
                .Include(o => o.TTs)
                .FirstOrDefault(o => o.Id == Id);

            if (orgSettings.Organization == null)
            {
                orgSettings.Organization = new RKNet_Model.TT.Organization();
                orgSettings.isNew = true;
            }

            orgSettings.TTs = db.TTs.ToList();
            return PartialView(orgSettings);
        }

        // Сохранение организации
        public IActionResult OrganizationSave(string orgjsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                orgjsn = orgjsn.Replace("%bkspc%", " ");
                var orgJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.organization>(orgjsn);

                // существующая организация
                if (orgJsn.id != 0)
                {
                    var organization = db.Organizations
                        .Include(o => o.TTs)
                        .FirstOrDefault(o => o.Id == orgJsn.id);

                    switch (orgJsn.attribute)
                    {
                        case "orgName":
                            if (!string.IsNullOrEmpty(orgJsn.name))
                            {
                                organization.Name = orgJsn.name;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Название организации заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existName = db.Organizations.FirstOrDefault(o => o.Name == organization.Name);
                            if (existName != null)
                            {
                                result.Ok = false;
                                result.Data = "Организация с названием \"" + existName.Name + "\" уже существует, введите другое имя.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "tts":
                            organization.TTs = new List<RKNet_Model.TT.TT>();
                            foreach (var item in orgJsn.items)
                            {
                                var tt = db.TTs.FirstOrDefault(t => t.Id == item.id);
                                organization.TTs.Add(tt);
                            }
                            break;

                            //case "yandexClient":
                            //    if (!string.IsNullOrEmpty(orgJsn.yandexClient))
                            //    {
                            //        organization.YandexLogin = orgJsn.yandexClient;
                            //    }
                            //    else
                            //    {
                            //        organization.YandexLogin = null;
                            //    }
                            //    break;

                            //case "yandexSecret":
                            //    if (!string.IsNullOrEmpty(orgJsn.yandexSecret))
                            //    {
                            //        organization.YandexPassword = orgJsn.yandexSecret;
                            //    }
                            //    else
                            //    {
                            //        organization.YandexPassword = null;
                            //    }
                            //    break;
                            //case "deliveryClubClient":
                            //    if (!string.IsNullOrEmpty(orgJsn.deliveryClubClient))
                            //    {
                            //        organization.DeliveryClubLogin = orgJsn.deliveryClubClient;
                            //    }
                            //    else
                            //    {
                            //        organization.DeliveryClubLogin = null;
                            //    }
                            //    break;

                            //case "deliveryClubSecret":
                            //    if (!string.IsNullOrEmpty(orgJsn.deliveryClubSecret))
                            //    {
                            //        organization.DeliveryClubPassword = orgJsn.deliveryClubSecret;
                            //    }
                            //    else
                            //    {
                            //        organization.DeliveryClubPassword = null;
                            //    }
                            //    break;
                    }

                    db.Organizations.Update(organization);
                }
                // новая организация
                else
                {
                    var organization = new RKNet_Model.TT.Organization();
                    var tts = new List<RKNet_Model.TT.TT>();

                    // торговые точки в организации
                    foreach (var tt in orgJsn.tts)
                    {
                        tts.Add(db.TTs.First(t => t.Id == tt.id));
                    }

                    // название ортганизации
                    if (!string.IsNullOrEmpty(orgJsn.name))
                    {
                        organization.Name = orgJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название организации заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existName = db.Organizations.FirstOrDefault(o => o.Name == organization.Name);
                    if (existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Организация с именем \"" + existName.Name + "\" уже существует, введите другое имя.";
                        return new ObjectResult(result);
                    }

                    // Яндекс Еда
                    //if (!string.IsNullOrEmpty(orgJsn.yandexClient))
                    //{
                    //    organization.YandexLogin = orgJsn.yandexClient;
                    //}
                    //else
                    //{
                    //    organization.YandexLogin = null;
                    //}
                    //if (!string.IsNullOrEmpty(orgJsn.yandexSecret))
                    //{
                    //    organization.YandexPassword = orgJsn.yandexSecret;
                    //}
                    //else
                    //{
                    //    organization.YandexPassword = null;
                    //}

                    // Delivery Club
                    //if (!string.IsNullOrEmpty(orgJsn.deliveryClubClient))
                    //{
                    //    organization.DeliveryClubLogin = orgJsn.deliveryClubClient;
                    //}
                    //else
                    //{
                    //    organization.DeliveryClubLogin = null;
                    //}
                    //if (!string.IsNullOrEmpty(orgJsn.deliveryClubSecret))
                    //{
                    //    organization.DeliveryClubPassword = orgJsn.deliveryClubSecret;
                    //}
                    //else
                    //{
                    //    organization.DeliveryClubPassword = null;
                    //}

                    organization.TTs = tts;
                    db.Organizations.Add(organization);
                }

                db.SaveChanges();
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }

        // Удаление организации
        public IActionResult OrganizationDelete(int Id)
        {
            try
            {
                var organization = db.Organizations
                    .Include(o => o.TTs)
                    .FirstOrDefault(o => o.Id == Id);

                db.Organizations.Remove(organization);
                db.SaveChanges();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Выбранные элементы коллекций на организации
        public IActionResult GetOrganizationItems(int Id, string selectId)
        {
            var organization = db.Organizations
                .Include(o => o.TTs)
                .FirstOrDefault(o => o.Id == Id);

            switch (selectId)
            {
                case "tts":
                    return new ObjectResult(organization.TTs);
                default:
                    return new ObjectResult("empty");
            }
        }

        // Id организации по имени
        public IActionResult GetOrganizationId(string Name)
        {
            var Id = db.Organizations.FirstOrDefault(o => o.Name == Name).Id;
            return new ObjectResult(Id);
        }

        // Типы ***********************************************************
        // Шапка + разметка для вывода
        public IActionResult Types()
        {
            return PartialView();
        }

        // Таблица типов
        public IActionResult TypesTable()
        {
            var ttTypes = db.TTtypes
                .Include(t => t.TTs.Where(t => t.CloseDate == null))
                .ToList();
            return PartialView(ttTypes);
        }

        // Редактор типов
        public IActionResult TypeEdit(int Id)
        {
            var typeSettings = new TTTypeSettings();

            typeSettings.TTType = db.TTtypes
                .Include(t => t.TTs)
                .FirstOrDefault(t => t.Id == Id);

            if (typeSettings.TTType == null)
            {
                typeSettings.TTType = new RKNet_Model.TT.Type();
                typeSettings.isNew = true;
            }

            typeSettings.TTs = db.TTs.ToList();
            return PartialView(typeSettings);
        }

        // Сохранение типа
        public IActionResult TypeSave(string typejsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                typejsn = typejsn.Replace("%bkspc%", " ");
                var typeJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.type>(typejsn);

                // существующий тип
                if (typeJsn.id != 0)
                {
                    var type = db.TTtypes
                        .Include(t => t.TTs)
                        .FirstOrDefault(t => t.Id == typeJsn.id);

                    switch (typeJsn.attribute)
                    {
                        case "typeName":
                            if (!string.IsNullOrEmpty(typeJsn.name))
                            {
                                type.Name = typeJsn.name;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Название типа заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existName = db.TTtypes.FirstOrDefault(t => t.Name == type.Name);
                            if (existName != null)
                            {
                                result.Ok = false;
                                result.Data = "Тип с именем \"" + existName.Name + "\" уже существует, введите другое имя.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "tts":
                            type.TTs = new List<RKNet_Model.TT.TT>();
                            foreach (var item in typeJsn.items)
                            {
                                var tt = db.TTs.FirstOrDefault(t => t.Id == item.id);
                                type.TTs.Add(tt);
                            }
                            break;
                    }

                    db.TTtypes.Update(type);
                }
                // новый тип
                else
                {
                    var type = new RKNet_Model.TT.Type();
                    var tts = new List<RKNet_Model.TT.TT>();

                    // торговые точки типа
                    foreach (var tt in typeJsn.tts)
                    {
                        tts.Add(db.TTs.First(t => t.Id == tt.id));
                    }

                    // название типа
                    if (!string.IsNullOrEmpty(typeJsn.name))
                    {
                        type.Name = typeJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название типа заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existName = db.TTtypes.FirstOrDefault(t => t.Name == type.Name);
                    if (existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Тип с именем \"" + existName.Name + "\" уже существует, введите другое имя.";
                        return new ObjectResult(result);
                    }

                    type.TTs = tts;
                    db.TTtypes.Add(type);
                }

                db.SaveChanges();
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }

        // Удаление типа
        public IActionResult TypeDelete(int Id)
        {
            try
            {
                var type = db.TTtypes
                    .Include(t => t.TTs)
                    .FirstOrDefault(t => t.Id == Id);

                db.TTtypes.Remove(type);
                db.SaveChanges();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Выбранные элементы коллекций на типе
        public IActionResult GetTypeItems(int Id, string selectId)
        {
            var type = db.TTtypes
                .Include(t => t.TTs)
                .FirstOrDefault(t => t.Id == Id);

            switch (selectId)
            {
                case "tts":
                    return new ObjectResult(type.TTs);
                default:
                    return new ObjectResult("empty");
            }
        }

        // Id типа по имени
        public IActionResult GetTypeId(string Name)
        {
            var Id = db.TTtypes.FirstOrDefault(t => t.Name == Name).Id;
            return new ObjectResult(Id);
        }

        // Камеры ***********************************************************
        public IActionResult GetCamPreview(int systemId, string cameraId)
        {
            try
            {
                var moduleNx = new module_NX.NX();
                var nxSystem = db.NxSystems.FirstOrDefault(s => s.Id == systemId);
                var getCamPicture = moduleNx.GetCameraPicture(nxSystem, DateTime.Now, cameraId, 300);

                if (getCamPicture.Ok)
                {
                    var cameraBitmap = getCamPicture.Data;

                    // изменяем размер (меняет пропорции рыбьего глаза)
                    Size newSize = new Size((int)(cameraBitmap.Height * 1.8), cameraBitmap.Height);
                    var imgResized = new Bitmap(cameraBitmap, newSize);

                    FileContentResult result;

                    using (var memStream = new MemoryStream())
                    {
                        imgResized.Save(memStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        result = this.File(memStream.GetBuffer(), "image/jpeg");
                    }
                    return result;
                }
                else
                {
                    return new ObjectResult(getCamPicture.ExceptionText);
                }
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }
        public IActionResult RemoveCamFromTT(int camId, int ttId)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                if (ttId > 0)
                {
                    var cam = db.NxCameras.FirstOrDefault(c => c.Id == camId);

                    db.NxCameras.Remove(cam);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.Message;
            }

            return new ObjectResult(result);
        }
        public IActionResult TTCamsPreview(int ttId, string jsn)
        {
            var camList = new List<RKNet_Model.VMS.NX.NxCamera>();

            var tt = db.TTs.Include(t => t.NxCameras).FirstOrDefault(t => t.Id == ttId);
            if (tt != null)
            {
                foreach (var cam in tt.NxCameras)
                {
                    camList.Add(db.NxCameras.Include(c => c.NxSystem).FirstOrDefault(c => c.Id == cam.Id));
                }
            }
            else
            {
                jsn = jsn.Replace("%pp%", "+");
                jsn = jsn.Replace("%bkspc%", " ");

                var ttcams = JsonConvert.DeserializeObject<List<ViewModels.Settings_TT.json.ttcams>>(jsn);
                foreach (var ttcam in ttcams)
                {
                    var cam = new RKNet_Model.VMS.NX.NxCamera();
                    cam.Name = ttcam.name;
                    cam.Guid = ttcam.id;
                    cam.NxSystem = db.NxSystems.FirstOrDefault(s => s.Id == ttcam.systemId);

                    camList.Add(cam);
                }
            }
            return PartialView(camList);
        }
        public IActionResult AddTTCam(int ttId)
        {
            var camView = new CamerasView();

            // выбранная ТТ
            camView.SelectedTT = db.TTs.FirstOrDefault(t => t.Id == ttId);
            camView.NxSystems = db.NxSystems.ToList();

            return PartialView(camView);
        }
        public IActionResult GetNxServers(int systemId)
        {
            var result = new RKNet_Model.Result<List<module_NX.NX.FullInfo.server>>();
            try
            {
                // запрос списка серверов NX системы
                var nxRequest = new RKNet_Model.Result<module_NX.NX.FullInfo>();
                var nxSystem = db.NxSystems.FirstOrDefault(s => s.Id == systemId);
                var nx = new module_NX.NX();

                nxRequest = nx.GetFullInfo(nxSystem);
                if (nxRequest.Ok)
                {
                    result.Data = nxRequest.Data.servers.OrderBy(s => s.name).ToList();
                }
                // ошибки запроса
                else
                {
                    result.Ok = false;
                    if (nxRequest.ErrorMessage.Contains("401"))
                    {
                        result.ErrorMessage = "неверное имя пользователя или пароль";
                        ModelState.AddModelError("User", "");
                        ModelState.AddModelError("Password", "");
                        ModelState.AddModelError("", result.ErrorMessage);
                    }
                    if (nxRequest.ErrorMessage.Contains("Этот хост неизвестен"))
                    {
                        result.ErrorMessage = "указанный адрес и/или порт недоступны";
                        ModelState.AddModelError("Host", "");
                        ModelState.AddModelError("Port", "");
                        ModelState.AddModelError("", result.ErrorMessage);
                    }

                    return new ObjectResult(result);
                }
            }
            // ошибки контроллера
            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
                return new ObjectResult(result);
            }

            return new ObjectResult(result);
        }
        public IActionResult ServerCamsPreview(int ttId, int systemId, string serverId)
        {
            var camView = new CamerasView();

            camView.NxSystemId = systemId;
            camView.SelectedTT = db.TTs.Include(t => t.NxCameras).FirstOrDefault(t => t.Id == ttId);
            camView.CamList = db.NxCameras.Include(c => c.TT).Where(c => c.TT.Id == ttId).OrderBy(c => c.Name).ToList();

            // запрос камер на сервере NX системы
            var nxRequest = new RKNet_Model.Result<module_NX.NX.FullInfo>();
            var nxSystem = db.NxSystems.FirstOrDefault(s => s.Id == systemId);
            var nx = new module_NX.NX();

            nxRequest = nx.GetFullInfo(nxSystem);
            if (nxRequest.Ok)
            {
                camView.ServerCams = nxRequest.Data.cameraUserAttributesList.Where(c => c.preferredServerId == serverId).ToList();
            }

            return PartialView(camView);
        }
        public IActionResult CameraChange(int ttId, int systemId, string camName, string camGuid, bool enabled)
        {
            var result = new RKNet_Model.Result<string>();

            try
            {
                var cam = db.NxCameras.Include(c => c.TT).FirstOrDefault(c => c.Guid == camGuid);
                var tt = db.TTs.Include(t => t.NxCameras).FirstOrDefault(t => t.Id == ttId);

                if (enabled)
                {
                    if (cam == null)
                    {
                        var newCamera = new RKNet_Model.VMS.NX.NxCamera();
                        newCamera.Name = camName;
                        newCamera.Guid = camGuid;
                        newCamera.NxSystem = db.NxSystems.FirstOrDefault(s => s.Id == systemId);
                        newCamera.TT = tt;

                        db.NxCameras.Add(newCamera);
                    }
                    else
                    {
                        cam.TT = tt;
                        db.NxCameras.Update(cam);
                    }
                }
                else
                {
                    db.NxCameras.Remove(cam);
                }

                db.SaveChanges();
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
            }

            return new ObjectResult(result);
        }
        public IActionResult GetTTCams(int ttId)
        {
            var camList = new List<RKNet_Model.VMS.NX.NxCamera>();
            var tt = db.TTs.Include(t => t.NxCameras).FirstOrDefault(t => t.Id == ttId);
            if (tt != null)
            {
                camList = tt.NxCameras.OrderBy(c => c.Name).ToList();
            }
            return new ObjectResult(camList);
        }
        public IActionResult GetCamGroupList()
        {
            var camGroups = db.CamGroups.OrderBy(g => g.Name).ToList();
            return new ObjectResult(camGroups);
        }
        public IActionResult GetCam(string camId)
        {
            var cam = db.NxCameras.Include(c => c.CamGroup).FirstOrDefault(c => c.Id == int.Parse(camId));
            return new ObjectResult(cam);
        }
        public IActionResult SaveCamProps(string camId, string groupId)
        {
            var result = new RKNet_Model.Result<string>();

            try
            {
                var cam = db.NxCameras.Include(c => c.CamGroup).FirstOrDefault(c => c.Id == int.Parse(camId));
                var group = db.CamGroups.FirstOrDefault(g => g.Id == int.Parse(groupId));

                if (cam == null)
                {
                    result.Ok = false;
                    result.ErrorMessage = $"камера с Id={camId} не найдена в базе данных";
                }

                if (group == null)
                {
                    result.Ok = false;
                    result.ErrorMessage = $"группа с Id={groupId} не найдена в базе данных";
                }

                cam.CamGroup = group;
                db.NxCameras.Update(cam);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }

            return new ObjectResult(result);
        }

        // Кассовые клиенты ***********************************************************
        // Шапка + разметка для вывода
        public IActionResult CashClients()
        {
            var cashClientsView = new CashClientsView();
            var versions = db.CashClientVersions.Select(v => new { v.Version, v.isActual }).ToList();
            foreach (var item in versions)
            {
                var ver = new RKNet_Model.CashClient.ClientVersion
                {
                    Version = item.Version,
                    isActual = item.isActual
                };
                cashClientsView.Versions.Add(ver);
            }

            cashClientsView.isAutoUpdate = db.ApiServerSettings.FirstOrDefault().CashClientsAutoUpdate;
            return PartialView(cashClientsView);
        }

        // Таблица клиентов
        public IActionResult ClientsTable()
        {
            var result = Models.ApiRequest.GetCashClients();
            var clientsView = new CashClientsView();

            if (result.Ok)
            {
                clientsView.Clients = result.Data;

                //cashClients.tts = db.TTs
                //.Include(t => t.Users)
                //.Include(t => t.CashStations)
                //.Where(t => !t.Closed)
                //.ToList();

                //var versions = db.CashClientVersions.Select(v => new { v.Version, v.isActual } ).ToList();
                var resultVersions = Models.ApiRequest.GetCashClientsVersions();

                if (resultVersions.Ok)
                {
                    if (resultVersions.Data != null)
                    {
                        clientsView.Versions = resultVersions.Data;
                    }
                }
            }
            return PartialView(clientsView);
        }

        // Обновить все клиенты
        public IActionResult UpdateAllClients(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new ObjectResult("Ошибка: версия обновления указана некорректно");
            }

            var result = Models.ApiRequest.UpdateAllClients(version);

            if (result.Ok)
                return new ObjectResult(result.Data);
            else
                return new ObjectResult(result.ErrorMessage);
        }

        // Обновить один клиент
        public IActionResult UpdateOneClient(string clientId, string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return new ObjectResult("Ошибка: версия обновления указана некорректно");
            }

            if (string.IsNullOrEmpty(clientId))
            {
                return new ObjectResult("Ошибка: Id клиента передан некорректно: " + clientId);
            }

            var result = Models.ApiRequest.UpdateOneClient(clientId, version);

            if (result.Ok)
                return new ObjectResult("ok");
            else
                return new ObjectResult(result.ErrorMessage);
        }

        // Отменить обновление одного клиента
        public IActionResult CancelUpdate(string clientId)
        {
            var result = Models.ApiRequest.CancelClientUpdate(clientId);

            if (result.Ok)
                return new ObjectResult("ok");
            else
                return new ObjectResult(result.ErrorMessage);
        }

        // Автообновление
        public IActionResult CashClientsAutoUpdate(bool isEnabled)
        {
            var result = Models.ApiRequest.CashClientsAutoUpdate(isEnabled);
            return new ObjectResult(result);
        }

        // Скачать версию кассового клиента

        public FileContentResult DownloadDistr(string version)
        {
            var clientVersion = db.CashClientVersions.FirstOrDefault(v => v.Version == version);
            if (clientVersion != null && clientVersion.UpdateFile != null)
            {
                HttpContext.Response.ContentType = "application/zip";
                HttpContext.Response.Headers.Add("Content-Disposition", "attachment");
                return File(clientVersion.UpdateFile, "application/zip", $"CashClient v{version}.zip");
            }
            return null;
        }

        //Загрузка новой версии кассового клиента в БД
        [HttpPost]
        public IActionResult UploadNewVersion(IFormFile file, string version, string actual)
        {
            Result<string> result = new Result<string>();
            // Проверки входных данных
            if (file == null)
            {
                result.Ok = false;
                result.Data = "Необходимо загрузить файл";
                return new ObjectResult(result);
            }
            if (version == null || version == "")
            {
                result.Ok = false;
                result.Data = "Необходимо указать версию";
                return new ObjectResult(result);
            }
            ClientVersion alreadyExist = db.CashClientVersions.FirstOrDefault(v => v.Version == version);
            if (alreadyExist != null)
            {
                result.Ok = false;
                result.Data =  $"Версия {version} уже существует в БД";
                return new ObjectResult(result);
            }
            if (actual != "on" && actual != null)
            {
                result.Ok = false;
                result.Data = "Неверно передаётся параметр actual";
                return new ObjectResult(result);
            }

            //Создание новой записи в БД
            try
            {
                ClientVersion clientVersion = new RKNet_Model.CashClient.ClientVersion();
                clientVersion.Version = version;
                using Stream fileStream = file.OpenReadStream();
                byte[] bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                clientVersion.UpdateFile = bytes;
                switch (actual)
                {
                    case "on":
                        foreach (var item in db.CashClientVersions)
                        {
                            item.isActual = false;
                        }
                        clientVersion.isActual = true;
                        break;
                    default:
                        clientVersion.isActual = false;
                        break;
                }
                db.CashClientVersions.Add(clientVersion);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Data = ex.Message;
                return new ObjectResult(result);
            }           
            result.Data = $"версия {version} успешно добавлена";
            return new ObjectResult(result);
        }
    }
}

