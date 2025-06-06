using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Portal.Models.JsonModels;
using Portal.Models.MSSQL;
using Portal.Models.MSSQL.Location;
using Portal.Models.MSSQL.Personality;
using Portal.Models.MSSQL.PersonalityVersions;
using Portal.ViewModels.Settings_Access.json;
using Portal.ViewModels.Settings_TT;
using RKNet_Model;
using RKNet_Model.Account;
using RKNet_Model.CashClient;
using RKNet_Model.TT;
using RKNet_Model.VMS.NX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace Portal.Controllers
{
    [Authorize(Roles = "settings,TTSettings")]
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
        public IActionResult TTsFactory()
        {
            return PartialView();
        }

        public IActionResult TTsOffice()
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

            TTVersions tVersions = new();

            List<LocationVersions> loc = dbSql.LocationVersions.Include(x => x.Location)
                                                               .Include(x => x.Entity)
                                                               .Include(x => x.Location.LocationType)
                                                               .Where(x => x.Actual == 1)
                                                               .Where(x => x.Location.LocationType.Guid == Guid.Parse("94AD659C-AF5B-4CA0-50AD-08DBDF6ABE84") 
                                                               || x.Location.LocationType.Guid == Guid.Parse("B0E427F9-8996-4C03-33C1-08DBDF713401")
                                                               || x.Location.LocationType.Guid == Guid.Parse("5E66963A-7767-4E51-84F9-8C320C6CE214")
                                                               || x.Location.LocationType.Guid == Guid.Parse("3DC24D14-FAE6-4993-A403-C4142755409A")).ToList();

            var tts = db.TTs.Include(t => t.Users)
                            .Include(t => t.CashStations)
                            .Include(t => t.NxCameras)
                            .Include(t => t.Type)
                            .Include(t => t.Organization)
                            .Where(t => !t.Closed)
                            .ToList();

            if (closedTT==false) { loc.RemoveAll(item => item.VersionEndDate != null && item.Location.Actual != 0); }
            if (ecTT==false) { loc.RemoveAll(item => item.Location?.LocationType?.Name == "УЦ"); }

            tVersions.LocationVersion = loc;
            tVersions.OldTT = tts;

            return PartialView(tVersions);
        }
        // Таблица ТТ для заводов
        public IActionResult TTsFactoryTable()
        {
            if (db.TTs is null)
            {
                return Ok();
            }

            List<LocationVersions> locationFactory = dbSql.LocationVersions
                .Include(x => x.Location)
                .Include(x => x.Location.LocationType)
                .Include(x => x.Entity)
                .Where(x => x.Actual == 1)
                .Where(x => x.Location.LocationType.Guid == Guid.Parse("80423E42-DC1E-4311-AD0B-08DCA4A09C33") || x.Location.LocationType.Guid == Guid.Parse("D3A0363D-2EC4-48E4-AD0C-08DCA4A09C33"))
                .ToList();

            return PartialView(locationFactory);
        }

        public IActionResult TTsOfficeTable()
        {
            if (db.TTs is null)
            {
                return Ok();
            }

            List<LocationVersions> locationFactory = dbSql.LocationVersions
                .Include(x => x.Location)
                .Include(x => x.Location.LocationType)
                .Include(x => x.Entity)
                .Where(x => x.Actual == 1)
                .Where(x => x.Location.LocationType.Guid == Guid.Parse("3810B715-2164-4524-F182-08DBF1A777FF") || x.Location.LocationType.Guid == Guid.Parse("8FE12BFC-1860-4B79-8763-81C984E2A643"))
                .ToList();

            return PartialView(locationFactory);
        }

        // Таблица версий ТТ
        public IActionResult TTsTableVersion(string locGuid)
        {
            if (db.TTs is null)
            {
                return Ok();
            }

            TTVersions tVersions = new();

            List<LocationVersions> loc = dbSql.LocationVersions.Include(x => x.Location)
                                                               .Include(x => x.Entity)
                                                               .Include(x => x.Location.LocationType)
                                                               .Where(x => x.Location.Guid == Guid.Parse(locGuid))
                                                               .ToList();

            var tts = db.TTs.Include(t => t.Users)
                            .Include(t => t.CashStations)
                            .Include(t => t.NxCameras)
                            .Include(t => t.Type)
                            .Include(t => t.Organization)
                            .Where(t => !t.Closed)
                            .ToList();


            tVersions.LocationVersion = loc;
            tVersions.OldTT = tts;

            return PartialView(tVersions);
        }
        // Редактор ТТ
        public IActionResult TTEdit(string ttGuid, string original)
         {
            TTVersionsEdit ttSettings = new();
            
            // Проверка на тип добавления записи (оригинал или версия)
            if(original == "1")
            {
                if (ttGuid != null && ttGuid != "undefined")
                {
                    ttSettings.LocationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                       .Include(x => x.Location.LocationType)
                                                                       .Include(x => x.Entity)
                                                                       .Where(x => x.Guid == Guid.Parse(ttGuid))
                                                                       .ToList();

                    int helper = (int)ttSettings.LocationVersion[0].Location.RKCode;

                    ttSettings.OldTT = db.TTs
                    .Include(t => t.Users)
                    .Include(t => t.CashStations)
                    .Include(t => t.NxCameras)
                    .Where(t => t.Restaurant_Sifr == helper)
                    .ToList();
                }
                else
                {
                    ttSettings.TTNew = true;
                    ttSettings.OldTT = db.TTs
                                         .Include(x => x.Users)
                                         .ToList();

                }
            } // Получаем информацию о точке, если создаем новую версию
            else if(original == "2")
            {
                ttSettings.LocationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                   .Include(x => x.Location.LocationType)
                                                                   .Include(x => x.Entity)
                                                                   .Where(x => x.Location.Guid == Guid.Parse(ttGuid) && x.Actual == 1)
                                                                   .ToList();

                ttSettings.OldTT = db.TTs
                    .Include(t => t.Users)
                    .Include(t => t.CashStations)
                    .Include(t => t.NxCameras)
                    .Where(t => t.Obd == ttSettings.LocationVersion[0].OBD)
                    .ToList();

                ttSettings.TTNew = true;
            }
            else
            {
                ttSettings.LocationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                   .Include(x => x.Location.LocationType)
                                                                   .Include(x => x.Entity)
                                                                   .Where(x => x.Location.Guid == Guid.Parse(ttGuid) && x.Actual == 1)
                                                                   .ToList();

                ttSettings.OldTT = db.TTs
                    .Include(t => t.Users)
                    .Include(t => t.CashStations)
                    .Include(t => t.NxCameras)
                    .Where(t => t.Obd == ttSettings.LocationVersion[0].OBD)
                    .ToList();

                ttSettings.TTNew = true;
            }

            ttSettings.Users = db.Users.ToList();
            ttSettings.locationTypes = dbSql.LocationTypes.ToList();
            ttSettings.Entities = dbSql.Entity.ToList();
            ttSettings.original = original;

            return PartialView(ttSettings);
        }

        public IActionResult TTFactoryAdd()
        {
            TTsFactoryEdit tsFactoryEdit = new TTsFactoryEdit();
            List<Entity> entity = dbSql.Entity.ToList();
            List<LocationVersions> locationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                           .Include(x => x.Location.LocationType)
                                                                           .Where(x => x.Actual == 1)
                                                                           .Where(x => x.Location.LocationType.Name == "Завод" || x.Location.LocationType.Name == "Цех")
                                                                           .ToList();
            List<LocationType> locationTypes = dbSql.LocationTypes.ToList();
            List<User> users = db.Users.ToList();
            tsFactoryEdit.entity = entity;
            tsFactoryEdit.locationTypes = locationTypes;
            tsFactoryEdit.location = locationVersion;
            tsFactoryEdit.users = users;

            return PartialView(tsFactoryEdit);
        }

        public IActionResult TTOfficeAdd()
        {
            TTsFactoryEdit tsFactoryEdit = new TTsFactoryEdit();
            List<Entity> entity = dbSql.Entity.ToList();
            List<LocationVersions> locationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                           .Include(x => x.Location.LocationType)
                                                                           .Where(x => x.Actual == 1)
                                                                           .Where(x => x.Location.LocationType.Name == "Офис" || x.Location.LocationType.Name == "Кабинет")
                                                                           .ToList();
            List<LocationType> locationTypes = dbSql.LocationTypes.ToList();
            List<User> users = db.Users.ToList();
            tsFactoryEdit.entity = entity;
            tsFactoryEdit.locationTypes = locationTypes;
            tsFactoryEdit.location = locationVersion;
            tsFactoryEdit.users = users;

            return PartialView(tsFactoryEdit);
        }

        public IActionResult TTOfficeEdit(string guid)
        {
            TTsFactoryEditData tsFactoryEdit = new TTsFactoryEditData();
            LocationVersions locationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                     .Include(x => x.Location.LocationType)
                                                                     .Include(x => x.Entity)
                                                                     .FirstOrDefault(x => x.Guid == Guid.Parse(guid));

            List<Models.MSSQL.Location.Location> locations = dbSql.Locations.Where(x => x.LocationType.Name == "Завод" || x.LocationType.Name == "Цех").ToList();
            List<Entity> entity = dbSql.Entity.ToList();
            List<LocationType> locationTypes = dbSql.LocationTypes.ToList();
            List<BindingLocationToUsers> bindingLocationToUsers = dbSql.BindingLocationToUsers.Where(x => x.LocationID == locationVersion.Guid).ToList();
            tsFactoryEdit.entity = entity;
            tsFactoryEdit.locationTypes = locationTypes;
            tsFactoryEdit.location = locationVersion;
            tsFactoryEdit.loca = locations;
            tsFactoryEdit.users = db.Users.ToList();
            tsFactoryEdit.pickedusers = bindingLocationToUsers;



            return PartialView(tsFactoryEdit);
        }

        public IActionResult TTFactoryEdit(string guid)
        {
            TTsFactoryEditData tsFactoryEdit = new TTsFactoryEditData();
            LocationVersions locationVersion = dbSql.LocationVersions.Include(x => x.Location)
                                                                     .Include(x => x.Location.LocationType)
                                                                     .Include(x => x.Entity)
                                                                     .FirstOrDefault(x => x.Guid == Guid.Parse(guid));

            List<Models.MSSQL.Location.Location> locations = dbSql.Locations.Where(x => x.LocationType.Name == "Завод" || x.LocationType.Name == "Цех").ToList();
            List<Entity> entity = dbSql.Entity.ToList();
            List<LocationType> locationTypes = dbSql.LocationTypes.ToList();
            List<BindingLocationToUsers> bindingLocationToUsers = dbSql.BindingLocationToUsers.Where(x => x.LocationID == locationVersion.Guid).ToList();
            tsFactoryEdit.entity = entity;
            tsFactoryEdit.locationTypes = locationTypes;
            tsFactoryEdit.location = locationVersion;
            tsFactoryEdit.loca = locations;
            tsFactoryEdit.users = db.Users.ToList();
            tsFactoryEdit.pickedusers = bindingLocationToUsers;

            

            return PartialView(tsFactoryEdit);
        }

        public IActionResult TTFactorySave(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                json = json.Replace("%bkspc%", " ");
                var ttJsn = JsonConvert.DeserializeObject<Portal.Models.JsonModels.TTsFactoryAdd>(json);

                LocationVersions locationVersions = dbSql.LocationVersions.Include(x => x.Location)
                                                                          .Include(x => x.Location.LocationType)
                                                                          .Include(x => x.Entity)
                                                                          .FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.guid));


                locationVersions.Location.Name = ttJsn.Name;
                locationVersions.Location.LocationType = dbSql.LocationTypes.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.type));
                locationVersions.Location.RKCode = 0;
                locationVersions.Location.AggregatorsCode = null;
                if (ttJsn.parent != "" && ttJsn.parent != null)
                {
                    locationVersions.Location.Parent = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.parent));
                }
                else
                {
                    locationVersions.Location.Parent = null;
                }
                locationVersions.Location.Latitude = Double.Parse(ttJsn.latitude.Replace(",", "."), CultureInfo.InvariantCulture);
                locationVersions.Location.Longitude = Double.Parse(ttJsn.longitude.Replace(",", "."), CultureInfo.InvariantCulture);
                locationVersions.Location.Actual = 1;


                locationVersions.Name = ttJsn.Name;
                locationVersions.OBD = null;
                locationVersions.Entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.entity));
                locationVersions.Actual = 1;
                locationVersions.VersionStartDate = DateTime.Parse(ttJsn.open);
                if (ttJsn.close != "")
                {
                    locationVersions.VersionEndDate = DateTime.Parse(ttJsn.close);
                }
                else
                {
                    locationVersions.VersionEndDate = null;
                }
                locationVersions.Address = ttJsn.address;

                List<Models.MSSQL.BindingLocationToUsers> toDelete = dbSql.BindingLocationToUsers.Where(x => x.LocationID == Guid.Parse(ttJsn.guid)).ToList();
                toDelete.ForEach(x =>
                {
                    dbSql.BindingLocationToUsers.Remove(x);
                });

                ttJsn.usersid.ForEach(x =>
                {
                    BindingLocationToUsers bindingLocationToUsers = new BindingLocationToUsers();
                    bindingLocationToUsers.LocationID = locationVersions.Guid;
                    bindingLocationToUsers.UserID = int.Parse(x);
                    dbSql.BindingLocationToUsers.Add(bindingLocationToUsers);
                });

                dbSql.SaveChanges();

                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }
        public IActionResult TTFactorySaveNew(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                json = json.Replace("%bkspc%", " ");
                var ttJsn = JsonConvert.DeserializeObject<Portal.Models.JsonModels.TTsFactoryAdd>(json);

                LocationVersions locationVersions = new LocationVersions();
                Models.MSSQL.Location.Location location = new Models.MSSQL.Location.Location();

                location.Name = ttJsn.Name;
                location.LocationType = dbSql.LocationTypes.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.type));
                location.RKCode = 0;
                location.AggregatorsCode = null;
                if (ttJsn?.parent != "" || ttJsn?.parent != null)
                {
                    location.Parent = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.parent));
                } 
                else
                {
                    location.Parent = null;
                }

                locationVersions.Location.Latitude = Double.Parse(ttJsn.latitude.Replace(",", "."), CultureInfo.InvariantCulture);
                locationVersions.Location.Longitude = Double.Parse(ttJsn.longitude.Replace(",", "."), CultureInfo.InvariantCulture);
                location.Actual = 1;

                locationVersions.Location = location;
                locationVersions.Name = ttJsn.Name;
                locationVersions.OBD = null;
                locationVersions.Entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.entity));
                locationVersions.Actual = 1;
                locationVersions.VersionStartDate = DateTime.Parse(ttJsn.open);
                if(ttJsn.close != "")
                {
                    locationVersions.VersionEndDate = DateTime.Parse(ttJsn.close);
                } 
                else
                {
                    locationVersions.VersionEndDate = null;
                }
                locationVersions.Address = ttJsn.address;

                dbSql.Locations.Add(location);
                dbSql.LocationVersions.Add(locationVersions);

                ttJsn.usersid.ForEach(x =>
                {
                    BindingLocationToUsers bindingLocationToUsers = new BindingLocationToUsers();
                    bindingLocationToUsers.LocationID = locationVersions.Guid;
                    bindingLocationToUsers.UserID = int.Parse(x);
                    dbSql.BindingLocationToUsers.Add(bindingLocationToUsers);
                });

                
                dbSql.SaveChanges();

                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }

        public IActionResult TTOfficeSave(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                json = json.Replace("%bkspc%", " ");
                var ttJsn = JsonConvert.DeserializeObject<Portal.Models.JsonModels.TTsFactoryAdd>(json);

                LocationVersions locationVersions = dbSql.LocationVersions.Include(x => x.Location)
                                                                          .Include(x => x.Location.LocationType)
                                                                          .Include(x => x.Entity)
                                                                          .FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.guid));


                locationVersions.Location.Name = ttJsn.Name;
                locationVersions.Location.LocationType = dbSql.LocationTypes.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.type));
                locationVersions.Location.RKCode = 0;
                locationVersions.Location.AggregatorsCode = null;
                if (ttJsn.parent != "" && ttJsn.parent != null)
                {
                    locationVersions.Location.Parent = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.parent));
                }
                else
                {
                    locationVersions.Location.Parent = null;
                }

                double latitude;
                if (Double.TryParse(ttJsn.latitude.Replace(",", "."), out latitude))
                {
                    locationVersions.Location.Latitude = latitude;
                }
                else
                {
                    locationVersions.Location.Latitude = null;
                }

                double longitude;
                if (Double.TryParse(ttJsn.latitude.Replace(",", "."), out longitude))
                {
                    locationVersions.Location.Longitude = longitude;
                }
                else
                {
                    locationVersions.Location.Longitude = null;
                }
                locationVersions.Location.Actual = 1;


                locationVersions.Name = ttJsn.Name;
                locationVersions.OBD = null;
                locationVersions.Entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.entity));
                locationVersions.Actual = 1;

                if (ttJsn.open != "")
                {
                    locationVersions.VersionStartDate = DateTime.Parse(ttJsn.open);
                }
                else
                {
                    locationVersions.VersionStartDate = null;
                }

                if (ttJsn.close != "")
                {
                    locationVersions.VersionEndDate = DateTime.Parse(ttJsn.close);
                }
                else
                {
                    locationVersions.VersionEndDate = null;
                }

                List<Models.MSSQL.BindingLocationToUsers> toDelete = dbSql.BindingLocationToUsers.Where(x => x.LocationID == Guid.Parse(ttJsn.guid)).ToList();
                toDelete.ForEach(x =>
                {
                    dbSql.BindingLocationToUsers.Remove(x);
                });

                ttJsn.usersid.ForEach(x =>
                {
                    BindingLocationToUsers bindingLocationToUsers = new BindingLocationToUsers();
                    bindingLocationToUsers.LocationID = locationVersions.Guid;
                    bindingLocationToUsers.UserID = int.Parse(x);
                    dbSql.BindingLocationToUsers.Add(bindingLocationToUsers);
                });

                dbSql.SaveChanges();

                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }
        public IActionResult TTOfficeSaveNew(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                json = json.Replace("%bkspc%", " ");
                var ttJsn = JsonConvert.DeserializeObject<Portal.Models.JsonModels.TTsFactoryAdd>(json);

                LocationVersions locationVersions = new LocationVersions();
                Models.MSSQL.Location.Location location = new Models.MSSQL.Location.Location();

                location.Name = ttJsn.Name;
                location.LocationType = dbSql.LocationTypes.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.type));
                location.RKCode = 0;
                location.AggregatorsCode = null;


                if (ttJsn?.parent != null)
                {
                    location.Parent = dbSql.Locations.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.parent));
                }
                else
                {
                    location.Parent = null;
                }

                double latitude;
                if (Double.TryParse(ttJsn.latitude.Replace(",", "."), out latitude))
                {
                    location.Latitude = latitude; 
                } else
                {
                    location.Latitude = null;
                }

                double longitude;
                if (Double.TryParse(ttJsn.latitude.Replace(",", "."), out longitude))
                {
                    location.Longitude = longitude;
                } else
                {
                    location.Longitude = null;
                }

                location.Actual = 1;

                locationVersions.Location = location;
                locationVersions.Name = ttJsn.Name;
                locationVersions.OBD = null;
                locationVersions.Entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.entity));
                locationVersions.Actual = 1;

                if (ttJsn.open != "")
                {
                    locationVersions.VersionStartDate = DateTime.Parse(ttJsn.open);
                }
                else
                {
                    locationVersions.VersionStartDate = null;
                }

                if (ttJsn.close != "")
                {
                    locationVersions.VersionEndDate = DateTime.Parse(ttJsn.close);
                }
                else
                {
                    locationVersions.VersionEndDate = null;
                }
                locationVersions.Address = ttJsn.address;

                dbSql.Locations.Add(location);
                dbSql.LocationVersions.Add(locationVersions);

                ttJsn.usersid.ForEach(x =>
                {
                    BindingLocationToUsers bindingLocationToUsers = new BindingLocationToUsers();
                    bindingLocationToUsers.LocationID = locationVersions.Guid;
                    bindingLocationToUsers.UserID = int.Parse(x);
                    dbSql.BindingLocationToUsers.Add(bindingLocationToUsers);
                });


                dbSql.SaveChanges();

                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
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
                if (ttJsn.Guid != "0" && ttJsn?.original == null)
                {
                    var tt = db.TTs
                        .Include(t => t.Users)
                        .Include(t => t.CashStations)
                        .Include(t => t.NxCameras)
                        .FirstOrDefault(t => t.Id == ttJsn.id);

                    LocationVersions ttNewBase = dbSql.LocationVersions.Include(x => x.Location)
                                                          .Include(x => x.Entity)
                                                          .Include(x => x.Location.LocationType)
                                                          .FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.Guid));

                    switch (ttJsn.attribute)
                    {
                        case "ttName":
                            if (!string.IsNullOrEmpty(ttJsn.name))
                            {
                                tt.Name = ttJsn.name;
                                ttNewBase.Name = ttJsn.name;
                                break;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Название торговой точки заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                        case "latitude":
                            ttNewBase.Location.Latitude =  Double.Parse(ttJsn.latitude.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                            break;

                        case "longitude":
                            ttNewBase.Location.Longitude = Double.Parse(ttJsn.longitude.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                            break;

                        case "ttRestaurantSifr":
                            tt.Restaurant_Sifr = ttJsn.restaurant_Sifr;
                            ttNewBase.Location.RKCode = ttJsn.restaurant_Sifr;
                            break;

                        case "ttAddress":
                            if (!string.IsNullOrEmpty(ttJsn.address))
                            {
                                tt.Address = ttJsn.address;
                                ttNewBase.Address = ttJsn.address;
                            }
                            else
                            {
                                tt.Address = null;
                                ttNewBase.Address = null;
                            }
                            break;

                        case "ttCode":
                            if (!string.IsNullOrEmpty(ttJsn.code))
                            {
                                tt.Code = int.Parse(ttJsn.code);
                                ttNewBase.Location.AggregatorsCode = int.Parse(ttJsn.code);
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
                                ttNewBase.OBD = int.Parse(ttJsn.obd);
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Код ОБД заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existObd = db.TTs.FirstOrDefault(t => t.Obd == tt.Obd);
                            var existNewBaseObd = dbSql.LocationVersions.FirstOrDefault(t => t.OBD == ttNewBase.OBD);
                            if (existObd != null && existNewBaseObd != null)
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
                                ttNewBase.VersionStartDate = open.Date;
                                ttNewBase.Location.Actual = 1;
                            }
                            else
                            {
                                tt.OpenDate = null;
                                ttNewBase.VersionStartDate = null;
                                ttNewBase.Location.Actual = 1;
                            }

                            break;

                        case "closeDate":

                            DateTime close;
                            Ok = DateTime.TryParse(ttJsn.closeDate, out close);

                            if (Ok)
                            {
                                ttNewBase.Location.Actual = 0;
                                tt.CloseDate = close.Date;
                                ttNewBase.VersionEndDate = close.Date;
                                
                            }
                            else
                            {
                                tt.CloseDate = null;
                                ttNewBase.VersionEndDate = null;
                                
                            }

                            break;

                        case "ttType":

                            foreach (var item in ttJsn.items)
                            {
                                ttNewBase.Location.LocationType = dbSql.LocationTypes.FirstOrDefault(x => x.Guid == Guid.Parse(item.guidOrg));
                                if (ttNewBase.Location.LocationType.Name == "Тестовая")
                                {
                                    tt.Type = db.TTtypes.FirstOrDefault(t => t.Name == "Спальник");
                                }
                            }

                            break;

                        case "ttOrganization":

                            foreach (var item in ttJsn.items)
                            {
                                /*tt.Organization = db.Organizations.FirstOrDefault(o => o.Id == item.id);*/
                                ttNewBase.Entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(item.guidOrg));
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
                            
                            case "cameras":
                                tt.NxCameras = new List<RKNet_Model.VMS.NX.NxCamera>();
                                foreach (var item in ttJsn.items)
                                {
                                    var camera = db.NxCameras.FirstOrDefault(c => c.Id == item.id);
                                    tt.NxCameras.Add(camera);
                                }
                                break;
                            }

                        if (tt != null)
                    {
                        db.TTs.Update(tt);
                        db.SaveChanges();
                    }
                    if (ttNewBase != null)
                    {
                        dbSql.Update(ttNewBase);
                        dbSql.SaveChanges();
                    }
                    
                }
                // новая ТТ или новая версия ТТ
                else
                {
                    var tt = new RKNet_Model.TT.TT();
                    var ttUsers = new List<User>();
                    var ttCameras = new List<RKNet_Model.VMS.NX.NxCamera>();
                    var ttCashes = new List<RKNet_Model.Rk7XML.CashStation>();

                    List<LocationVersions> locVersions = dbSql.LocationVersions.Include(x => x.Location)
                                                                               .Include(x => x.Location.LocationType)
                                                                               .Include(x => x.Entity)
                                                                               .ToList();

                    LocationVersions locversion = new();
                    Models.MSSQL.Location.Location location = new();
                    if (ttJsn.original != null)
                    {
                        location = dbSql.LocationVersions.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.Guid)).Location;
                    }
                    
                    TT ttFromOldBD = new();
                    if (ttJsn?.original != null && ttJsn.Guid != "0")
                    {
                        var helper = locVersions.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.Guid)).Name;
                        ttFromOldBD = db.TTs.FirstOrDefault(x => x.Name == helper);
                    }
                    
                    if(ttJsn.original == null)
                    {
                        foreach (var item in ttJsn.users)
                        {
                            tt.Users.Add(db.Users.First(u => u.Id == item.id));
                        }
                    }
                    else
                    {
                        ttFromOldBD.Users = new List<User>();
                        foreach (var item in ttJsn.users)
                        {
                            User user = db.Users.FirstOrDefault(u => u.Id == item.id);
                            ttFromOldBD.Users.Add(user);
                        }
                    }
                    
                    // тип новой тт
                    if (!string.IsNullOrEmpty(ttJsn.type))
                    {
                        location.LocationType = dbSql.LocationTypes.FirstOrDefault(t => t.Guid == Guid.Parse(ttJsn.type));

                        if (location.LocationType.Name == "Тестовая")
                        {
                            ttFromOldBD.Type = db.TTtypes.FirstOrDefault(t => t.Name == "Спальник");
                        } else {
                            ttFromOldBD.Type = db.TTtypes.FirstOrDefault(t => t.Name == location.LocationType.Name);
                        }

                        if(location.Latitude != null)
                        {
                            location.Latitude = Double.Parse(ttJsn.latitude.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                        }

                        if (location.Longitude != null)
                        {
                            location.Longitude = Double.Parse(ttJsn.latitude.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                        }

                        
                    }

                    // организация новой тт
                    if (!string.IsNullOrEmpty(ttJsn.organization))
                    {
                        locversion.Entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.organization));
                    }

                    // имя тт
                    if (!string.IsNullOrEmpty(ttJsn.name))
                    {
                        locversion.Name = ttJsn.name;
                        if(ttJsn?.original != null)
                        {
                            ttFromOldBD.Name = ttJsn.name;
                        }
                        else
                        {
                            tt.Name = ttJsn.name;
                        }
                        location.Name = ttJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название тороговой точки заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    // код RK
                    if(ttJsn?.original != null)
                    {
                        ttFromOldBD.Restaurant_Sifr = ttJsn.restaurant_Sifr;
                    }
                    else
                    {
                        tt.Restaurant_Sifr = ttJsn.restaurant_Sifr;
                    }
                    location.RKCode = ttJsn.restaurant_Sifr;

                    // Адрес ТТ
                    if (!string.IsNullOrEmpty(ttJsn.address))
                    {
                        locversion.Address = ttJsn.address;
                        if(ttJsn?.original != null)
                        {
                            ttFromOldBD.Address = ttJsn.address;
                        }
                        else
                        {
                            tt.Address = ttJsn.address;
                        }
                    }
                    else
                    {
                        tt.Address = null;
                        locversion.Address = null;
                    }

                    // код ТТ
                    if (!string.IsNullOrEmpty(ttJsn.code))
                    {
                        if(ttJsn?.original != null)
                        {
                            ttFromOldBD.Code = int.Parse(ttJsn.code);
                            location.AggregatorsCode = int.Parse(ttJsn.code);
                        }
                        else
                        {
                            tt.Code = int.Parse(ttJsn.code);
                            location.AggregatorsCode = int.Parse(ttJsn.code);
                        }
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Код торговой точки заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existCode = db.TTs.FirstOrDefault(t => t.Code == tt.Code);
                    if (existCode != null && ttJsn?.original == null)
                    {
                        result.Ok = false;
                        result.Data = "Торговая точка с кодом \"" + existCode.Code + "\" уже существует, введите другой код.";
                        return new ObjectResult(result);
                    }

                    // Код ОБД
                    if (!string.IsNullOrEmpty(ttJsn.obd))
                    {
                        if(ttJsn?.original != null)
                        {
                            ttFromOldBD.Obd = int.Parse(ttJsn.obd);
                        }
                        else
                        {
                            tt.Obd = int.Parse(ttJsn.obd);
                        }
                        locversion.OBD = int.Parse(ttJsn.obd);
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Код ОБД заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existObd = db.TTs.FirstOrDefault(t => t.Obd == tt.Obd);
                    if (existObd != null && ttJsn?.original == null)
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
                        if(ttJsn?.original != null)
                        {
                            ttFromOldBD.OpenDate = open.Date;
                        }
                        else
                        {
                            tt.OpenDate = open.Date;
                        }
                        location.Actual = 1;
                        locversion.VersionStartDate = open.Date;
                    }
                    else
                    {
                        tt.OpenDate = null;
                        locversion.VersionStartDate = null;
                    }

                    // Дата закрытия
                    DateTime close;
                    Ok = DateTime.TryParse(ttJsn.closeDate, out close);

                    if (Ok)
                    {
                        if (ttJsn?.original != null)
                        {
                            ttFromOldBD.CloseDate = close.Date;
                            locversion.VersionEndDate = close.Date;
                            location.Actual = 0;
                        }
                        else
                        {
                           tt.CloseDate = close.Date;
                           locversion.VersionEndDate = close.Date;
                           location.Actual = 0;
                        }
                    }
                    else
                    {
                        tt.CloseDate = null;
                        locversion.VersionEndDate = null;
                    }

                    // интеграции
                    
                    if (ttJsn?.original != null)
                    {
                        ttFromOldBD.YandexEda = ttJsn.yandexEda;
                        ttFromOldBD.DeliveryClub = ttJsn.deliveryClub;
                    }
                    else
                    {
                        tt.YandexEda = ttJsn.yandexEda;
                        tt.DeliveryClub = ttJsn.deliveryClub;
                    }

                        // Кассы на новой ТТ
                        if (ttJsn.cashes != null)
                    {
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
                                // добавляем кассу в бд
                                cash.Name = item.Name;
                                cash.Ip = item.Ip.ToString();
                                cash.TT = ttFromOldBD;
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
                    }

                    if (ttJsn.original == null)
                    {
                            ttCameras = new List<RKNet_Model.VMS.NX.NxCamera>();
                            foreach (var cam in ttJsn.cameras)
                            {
                                var nxCamera = new RKNet_Model.VMS.NX.NxCamera();
                                var nxCam = db.NxCameras.FirstOrDefault(x => x.Guid == cam.id);
                                if (nxCam != null)
                                {
                                    nxCamera.Name = cam.name;
                                    nxCamera.Guid = cam.id;
                                    nxCamera.NxSystem = db.NxSystems.FirstOrDefault(s => s.Id == cam.systemId);
                                    nxCamera.CamGroup = nxCam.CamGroup;
                                }
                                ttCameras.Add(nxCamera);
                            }
                            tt.NxCameras = ttCameras;
                    }

                    locversion.Location = location;
                        locversion.Actual = 1;
                        if (ttJsn.original == null)
                        {
                            db.TTs.Add(tt);
                        }
                        
                        if (ttJsn.original != null)
                        {
                            var helper = dbSql.LocationVersions.Include(x => x.Location).FirstOrDefault(x => x.Guid == Guid.Parse(ttJsn.Guid));
                            foreach (var item in dbSql.LocationVersions.Where(x => x.Location.Guid == helper.Location.Guid && x.Actual == 1))
                            {
                                item.Actual = 0;
                            }
                        }
                        
                        if (locversion != null)
                        {
                            dbSql.Add(locversion);
                            dbSql.SaveChanges();
                            db.SaveChanges();
                        }
                    
                }
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }
        // Выбранные элементы коллекций на ТТ
        public IActionResult GetTTItems(int ttId, string selectId, string ttGuid)
        {
            var tt = db.TTs
                .Include(t => t.Users)
                .Include(t => t.CashStations)
                .Include(t => t.NxCameras)
                .Include(t => t.Organization)
                .Include(t => t.Type)
                .FirstOrDefault(t => t.Id == ttId);

            var newTT = dbSql.LocationVersions.Include(x => x.Location)
                                              .Include(x => x.Location.LocationType)
                                              .Include(x => x.Entity)
                                              .FirstOrDefault(x => x.Guid == Guid.Parse(ttGuid));

            switch (selectId)
            {
                case "users":
                    return new ObjectResult(tt.Users);
                case "cashes":
                    return new ObjectResult(tt.CashStations);
                case "cameras":
                    return new ObjectResult(tt.NxCameras);
                case "ttType":
                    return new ObjectResult(newTT.Location.LocationType);
                case "ttOrganization":
                    return new ObjectResult(newTT.Entity);
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
            var model = new EntityLocationModel
            {
                Entities = dbSql.Entity != null ? dbSql.Entity.ToList() : new List<Entity>(),
                LocationVersions = dbSql.LocationVersions != null
                    ? dbSql.LocationVersions.Include(t => t.Entity).Include(t => t.Location).ToList()
                    : new List<LocationVersions>()
            };
            return PartialView(model);
        }

        // Редактор организаций 
        public IActionResult OrganizationEdit(string id)
        {
            if (id != null)
            {
                var model = new EntityLocationModel
                {
                    Entities = dbSql.Entity.Where(c => c.Guid == Guid.Parse(id)).ToList(),
                    LocationVersions = dbSql.LocationVersions.Include(t => t.Entity)
                                                                        .Include(t => t.Location)
                                                                        .ToList(),
                    New = 0
                };

                model.folderStatus = FolderIsExist(model.Entities[0].Name);

                return PartialView(model);
            }
            else
            {
                var model = new EntityLocationModel
                {
                    New = 1,
                    Entities = dbSql.Entity.ToList(),
                    LocationVersions = dbSql.LocationVersions.Include(t => t.Entity)
                                                                        .Include(t => t.Location)
                                                                        .ToList()  
                };

                model.folderStatus = FolderIsExist(model.Entities[0].Name);

                return PartialView(model);
            }
        }

        public static bool CreateFolder(string folderName)
        {

            var invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedName = new string(folderName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            string folderPath = "\\\\fs1\\LLWork\\Розница. Документы\\" + sanitizedName;

            try
            {
                Directory.CreateDirectory(folderPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании папки: {ex.Message}");
            }

            return true;
        }
        

        public static bool DeleteFolder(string folderName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedName = new string(folderName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            string folderPath = "\\\\fs1\\LLWork\\Розница. Документы\\" + sanitizedName;

            try
            {
                Directory.Delete(folderPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании папки: {ex.Message}");
            }

            return true;
        }

        public static bool FolderIsExist(string folderName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            string sanitizedName = new string(folderName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            string folderPath = "\\\\fs1\\LLWork\\Розница. Документы\\" + sanitizedName;

            try
            {
                bool folderExists = Directory.Exists(folderPath);

                if (folderExists == false)
                {
                    return false;
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке папки: {ex.Message}");
            }

            return true;
        }

        // Сохранение организации
        public IActionResult OrganizationSave(string orgjsn)
        {
            
            var result = new RKNet_Model.Result<string>();
            try
            {
                orgjsn = orgjsn.Replace("%bkspc%", " ");
                organization org = JsonConvert.DeserializeObject<organization>(orgjsn);
                if(org.Name == "")
                {
                    throw new Exception("Введите корректное имя");
                }

                Entity entity;
                if (org.Guid.ToString() == "0")
                {
                    entity = dbSql.Entity.FirstOrDefault(x => x.Guid.ToString() == org.Guid.ToString());
                }
                else
                {
                    entity = dbSql.Entity.FirstOrDefault(x => x.Guid == Guid.Parse(org.Guid));
                }
                
                if(entity != null)
                {
                    entity.Name = org.Name;
                    entity.Owner = org.Owner;
                }
                else
                {
                    Entity entity1 = new();
                    entity1.Name = org.Name;
                    entity1.Owner = org.Owner;
                    dbSql.Add(entity1);

                }

                var folderExist = FolderIsExist(org.Name);

                // Если галка нажата === папка нужна
                if (org.Checked && !folderExist)
                {
                    CreateFolder(org.Name);
                }

                if (!org.Checked && folderExist)
                {
                    DeleteFolder(org.Name);
                }

                dbSql.SaveChanges();
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.Message;

            }
            return new ObjectResult(result);
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

        public IActionResult JobTitles()
        {
            return PartialView();
        }

        // Таблица типов
        public IActionResult TypesTable()
        {
            List<Models.MSSQL.Location.Location> location = dbSql.Locations.Include(x => x.LocationType)
                                                     .ToList();

            List<LocationType> locationType = dbSql.LocationTypes.ToList();

            LocationTypeAndCountLocation locationTypeAndCountLocations = new();
            locationTypeAndCountLocations.Location = location;
            locationTypeAndCountLocations.LocationType = locationType;
            

            return PartialView(locationTypeAndCountLocations);
        }

        public IActionResult JobTitlesAdd()
        {
            return PartialView();
        }

        public IActionResult JobTitlesEdit(string guid)
        {
            JobTitle jobTitle = dbSql.JobTitles.FirstOrDefault(x => x.Guid == Guid.Parse(guid));

            return PartialView(jobTitle);
        }

        public IActionResult JobTitlesSaveNew(string job)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                if(dbSql.JobTitles.FirstOrDefault(x => x.Name.ToLower() == job.ToLower()) != null)
                {
                    throw new Exception("Данная должность была создана ранее!");
                }

                JobTitle jobTitle = new JobTitle();
                jobTitle.Name = job;
                dbSql.JobTitles.Add(jobTitle);
                dbSql.SaveChanges();
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }

        public IActionResult JobTitlesSave(string json)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {
                json = json.Replace("%bkspc%", " ");
                var ttJsn = JsonConvert.DeserializeObject<Portal.Models.MSSQL.Personality.JobTitle>(json);

                if(ttJsn.Name != "" && ttJsn.Guid.ToString() != "")
                {
                    JobTitle jobTitle = dbSql.JobTitles.FirstOrDefault(x => x.Guid == ttJsn.Guid);
                    jobTitle.Name = ttJsn.Name;
                    dbSql.SaveChanges();
                }
                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
        }

        public IActionResult JobTitlesTable()
        {
            List<Portal.Models.MSSQL.Personality.JobTitle> jobTitles = dbSql.JobTitles.ToList();
            return PartialView(jobTitles);
        }

        // Редактор типов
        public IActionResult TypeEdit(string Id)
        {
            LocationTypeAndCountLocation locationTypeAndCountLocations = new();

            if (Id != null)
            {
                locationTypeAndCountLocations.LocationType = dbSql.LocationTypes.Where(x => x.Guid == Guid.Parse(Id)).ToList();
                locationTypeAndCountLocations.Location = dbSql.Locations.Include(x => x.LocationType).ToList();
                locationTypeAndCountLocations.isNew = false;
            }
            else
            {
                locationTypeAndCountLocations.LocationType = null;
                locationTypeAndCountLocations.Location = dbSql.Locations.Include(x => x.LocationType).ToList();
                locationTypeAndCountLocations.isNew = true;
            }

            return PartialView(locationTypeAndCountLocations);
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
                if (typeJsn.id != "0")
                {
                    LocationType locType = dbSql.LocationTypes.FirstOrDefault(x => x.Guid == Guid.Parse(typeJsn.id));

                    if (!string.IsNullOrEmpty(typeJsn.name))
                    {
                        locType.Name = typeJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название типа заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existName = dbSql.LocationTypes.FirstOrDefault(t => t.Name == locType.Name);
                    if (existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Тип с именем \"" + existName.Name + "\" уже существует, введите другое имя.";
                        return new ObjectResult(result);
                    }
                           
                    dbSql.LocationTypes.Update(locType);
                }
                // новый тип
                else
                {
                    LocationType locType = new();

                    // название типа
                    if (!string.IsNullOrEmpty(typeJsn.name))
                    {
                        locType.Name = typeJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название типа заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existName = dbSql.LocationTypes.FirstOrDefault(t => t.Name == locType.Name);
                    if (existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Тип с именем \"" + existName.Name + "\" уже существует, введите другое имя.";
                        return new ObjectResult(result);
                    }
                    dbSql.LocationTypes.Add(locType);
                }

                dbSql.SaveChanges();
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
            }
            return new ObjectResult(result);
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
                ClientVersion clientVersion = new();
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

