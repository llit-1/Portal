using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RKNet_Model.Account;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using RKNet_Model.TT;

namespace Portal.Controllers
{
    [Authorize(Roles = "settings")]
    public class AccessController : Controller
    {
        private DB.SQLiteDBContext db;
        public AccessController(DB.SQLiteDBContext context)
        {
            db = context;
        }

        // Пользователи
        public IActionResult Users()
        {
            var users = db.Users.OrderBy(u => u.Name).ToList();
            return PartialView(users);
        }

        // Группы
        public IActionResult Groups()
        {
            var groups = db.Groups.OrderBy(g => g.Name).ToList();
            return PartialView(groups);
        }

        // Точки
        public IActionResult TTs()
        {            
            try
            {
                var tts = db.TTs.ToList();
                return PartialView(tts);
            }

            catch(Exception e)
            {
                return new ObjectResult(e.Message);
            }

            
        }

        // Редактор ТТ
        public IActionResult TTEdit(int ttId)
        {
            var ttEdit = new ViewModels.Access.TTEditView();
            
            try
            {
                ttEdit.TT = db.TTs.Include(c => c.CashStations).FirstOrDefault(t => t.Id == ttId);
                ttEdit.Users = db.Users.Include(t => t.TTs).ToList(); 

                return PartialView(ttEdit);
            }
            catch(Exception e)
            {
                return new ObjectResult(e.Message);
            }
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
                    requestResult.Data = "Касса, " + systemInfo2.restaurant.name;
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

        // Сохранение ТТ
        public IActionResult TTSave(string jsn)
        {
            try
            {
                jsn = jsn.Replace("pp", " ");
                var ttEdit = JsonConvert.DeserializeObject<ViewModels.Access.TTEditView>(jsn);
                var tt = db.TTs.Include(u => u.Users).Include(c => c.CashStations).FirstOrDefault(t => t.Id == ttEdit.ttId);
                
                // обновляем пользователей
                tt.Users = new List<User>();
                foreach(var userId in ttEdit.usersIds)
                {
                    var user = db.Users.FirstOrDefault(u => u.Id == userId);
                    tt.Users.Add(user);
                }

                // обновляем кассы

                // удаляем кассы
                var cashesDelete = tt.CashStations.Where(c => !ttEdit.cashes.Select(i => i.Id).Contains(c.Id)).ToList();
                db.CashStations.RemoveRange(cashesDelete);                
                
                foreach (var item in ttEdit.cashes)
                {
                    IPAddress ip;
                    var correctIp = IPAddress.TryParse(item.Ip, out ip);
                    if (!correctIp)
                    {
                        return new ObjectResult("для кассы " + item.Name + " указан некорректный ip адрес");
                    }

                    var cash = new RKNet_Model.Rk7XML.CashStation();
                                        
                    // новая касса
                    if (item.Id == 0)
                    {
                        // проверяем есть ли уже касса с таким ip в бд
                        var cashExist = db.CashStations.Where(c => c.Ip == ip.ToString()).Count();
                        if (cashExist > 0)
                        {
                            var existCash = db.CashStations.Include(t => t.TT).FirstOrDefault(c => c.Ip == ip.ToString());
                            return new ObjectResult("Касса с данным ip-адресом уже привязана к точке " + existCash.TT.Name + ", изменения не будут сохранены.");
                        }
                            
                        // добавляем кассу в бд
                        cash.Name = item.Name;
                        cash.Ip = ip.ToString();
                        cash.TT = tt;
                        db.CashStations.Add(cash);
                    }
                    // существующая касса
                    else
                    {                        
                        cash = db.CashStations.FirstOrDefault(c => c.Id == item.Id);
                        cash.Name = item.Name;
                        cash.Ip = ip.ToString();
                        db.CashStations.Update(cash);
                    }                    
                }

                db.TTs.Update(tt);
                db.SaveChanges();
                
                return new ObjectResult("Успешно сохранено");
            }
            catch(Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Добавление пользователя
        public IActionResult AddUser()
        {
            return PartialView();
        }
        public IActionResult UserCreate()
        {
            var userRights = new ViewModels.UserRights();
            userRights.User = new User();
            userRights.Roles = db.Roles.OrderBy(r => r.Name).ToList();
            userRights.Groups = db.Groups.OrderBy(g => g.Name).ToList();
            userRights.TTs = db.TTs.Where(t => !t.Closed).ToList();
            userRights.newItem = true;

            return PartialView("UserEdit", userRights);
        }

        // Добавление группы
        public IActionResult AddGroup()
        {
            return PartialView();
        }
        public IActionResult GroupCreate()
        {
            var userRights = new ViewModels.UserRights();
            userRights.Group = new Group();
            userRights.Roles = db.Roles.OrderBy(r => r.Name).ToList();
            userRights.Users = db.Users.OrderBy(u => u.Name).ToList();
            userRights.newItem = true;

            return PartialView("GroupEdit", userRights);
        }

        // Редактор пользователя
        public IActionResult UserEdit(string userId)
        {
            var userRights = new ViewModels.UserRights();

            userRights.User = db.Users.Include(u => u.Roles).Include(u => u.Groups).Include(u => u.TTs).Include(u => u.Reports).First(u => u.Id == int.Parse(userId));
            userRights.Roles = db.Roles.OrderBy(r => r.Name).ToList();
            userRights.Groups = db.Groups.OrderBy(g => g.Name).ToList();
            userRights.TTs = db.TTs.Where(t => !t.Closed).ToList();

            return PartialView(userRights);
        }

        // Изменение группы
        public IActionResult GroupEdit(string groupId)
        {
            var userRights = new ViewModels.UserRights();
            if (groupId != "0")
            {
                userRights.Group = db.Groups.Include(g => g.Users).Include(g => g.Roles).First(u => u.Id == int.Parse(groupId));
            }
            userRights.Users = db.Users.OrderBy(u => u.Name).ToList();
            userRights.Roles = db.Roles.OrderBy(r => r.Name).ToList();            

            return PartialView(userRights);
        }

        // Сохранение пользователя
        public IActionResult UserSave(string json)
        {
            json = json.Replace(" ", "+");
            json = json.Replace("%bkspc%", " ");
            var userView = JsonConvert.DeserializeObject<ViewModels.userJsn>(json);

            // существующий пользователь
            if (userView.id != 0) 
            {
                var user = db.Users.Include(u => u.Roles).Include(u => u.Groups).Include(t => t.TTs).Include(r => r.Reports).FirstOrDefault(u => u.Id == userView.id);
                var userRoles = new List<Role>();
                var userGroups = new List<Group>();
                var userTTs = new List<TT>();

                foreach (var roleId in userView.roles)
                {
                    userRoles.Add(db.Roles.First(r => r.Id == roleId));
                }

                foreach (var groupId in userView.groups)
                {
                    userGroups.Add(db.Groups.First(g => g.Id == groupId));
                }

                foreach (var ttId in userView.tts)
                {
                    userTTs.Add(db.TTs.First(t => t.Id == ttId));
                }

                var userReports = db.UserReports.FirstOrDefault(r => r.User.Id == user.Id);
                if (userReports == null)
                {
                    userReports = new RKNet_Model.Reports.UserReport();
                    userReports.UserId = user.Id;
                    userReports.ProfitFree = userView.profitFree;
                    userReports.ProfitPro = userView.profitPro;
                    db.UserReports.Add(userReports);
                }
                else
                {
                    user.Reports.ProfitFree = userView.profitFree;
                    user.Reports.ProfitPro = userView.profitPro;
                }
                    

                user.Name = userView.name;
                user.Login = userView.login;
                user.JobTitle = userView.job;
                user.Mail = userView.mail;
                user.Roles = userRoles;
                user.Groups = userGroups;
                user.TTs = userTTs;
                user.AllTT = userView.alltt;
                user.AdUser = userView.aduser;                
                if (user.AdUser)
                {
                    user.Password = "";
                }
                else
                {
                    user.Password = userView.password;
                }
                
                db.Users.Update(user);                
            }
            // новый пользователь
            else
            {
                var user = new User();
                var userRoles = new List<Role>();
                var userGroups = new List<Group>();
                var userTTs = new List<TT>();                

                foreach (var roleId in userView.roles)
                {
                    userRoles.Add(db.Roles.First(r => r.Id == roleId));
                }

                foreach (var groupId in userView.groups)
                {
                    userGroups.Add(db.Groups.First(g => g.Id == groupId));
                }

                foreach (var ttId in userView.tts)
                {
                    userTTs.Add(db.TTs.First(t => t.Id == ttId));
                }

                user.Name = userView.name;
                user.Login = userView.login;
                user.JobTitle = userView.job;
                user.Password = userView.password;
                user.Mail = userView.mail;
                user.Roles = userRoles;
                user.Groups = userGroups;
                user.TTs = userTTs;
                user.AllTT = userView.alltt;
                user.AdUser = userView.aduser;
                user.Reports.ProfitFree = userView.profitFree;
                user.Reports.ProfitPro = userView.profitPro;

                db.Users.Add(user);
                db.UserReports.Add(user.Reports);
            }

            db.SaveChanges();

            return new ObjectResult("успешно сохранено");
        }

        // Сохранение группы
        public IActionResult GroupSave(string jsn)
        {
            jsn = jsn.Replace("pp", " ");
            var groupView = JsonConvert.DeserializeObject<ViewModels.groupJsn>(jsn);

            if (groupView.id != 0)
            {
                var group = db.Groups.Include(g => g.Users).Include(g => g.Roles).First(g => g.Id == groupView.id);
                var groupRoles = new List<Role>();
                var groupUsers = new List<User>();

                foreach (var roleId in groupView.roles)
                {
                    groupRoles.Add(db.Roles.First(r => r.Id == roleId));
                }

                foreach (var userId in groupView.users)
                {
                    groupUsers.Add(db.Users.First(u => u.Id == userId));
                }

                group.Name = groupView.name;
                group.Description = groupView.description;
                group.Roles = groupRoles;
                group.Users = groupUsers;

                db.Groups.Update(group);
            }
            else
            {
                var group = new Group();
                var groupRoles = new List<Role>();
                var groupUsers = new List<User>();

                foreach (var roleId in groupView.roles)
                {
                    groupRoles.Add(db.Roles.First(r => r.Id == roleId));
                }

                foreach (var userId in groupView.users)
                {
                    groupUsers.Add(db.Users.First(u => u.Id == userId));
                }

                group.Name = groupView.name;
                group.Description = groupView.description;
                group.Roles = groupRoles;
                group.Users = groupUsers;

                db.Groups.Add(group);
            }

            db.SaveChanges();

            return new ObjectResult("успешно сохранено");
        }

        // Удаление пользователя
        public IActionResult UserDelete(int Id)
        {
            var user = db.Users.Include(r => r.Roles).Include(g => g.Groups).Include(t => t.TTs).Include(u => u.Reports).FirstOrDefault(u => u.Id == Id);
            db.Users.Remove(user);
            db.UserReports.Remove(user.Reports);
            db.SaveChanges();

            var result = "Пользователь " + user.Name + " удален";
            return new ObjectResult(result);
        }

        // Удаление группы
        public IActionResult GroupDelete(int Id)
        {
            var group = db.Groups.Include(g => g.Roles).Include(g => g.Users).First(g => g.Id == Id);
            var result = "Группа " + group.Name + " удалена";
            db.Groups.Remove(group);
            db.SaveChanges();

            return new ObjectResult(result);
        }
    }

    internal class userTT
    {
        public int id;
        public bool all;
        public int[] tts;
    }

    
}
