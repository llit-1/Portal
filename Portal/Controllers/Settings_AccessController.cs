using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Portal.ViewModels.Settings_Access;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using RKNet_Model.Account;
using System.ComponentModel.DataAnnotations;

namespace Portal.Controllers
{
    [Authorize(Roles = "settings")]
    public class Settings_AccessController : Controller
    {
        private DB.SQLiteDBContext db;        
        public Settings_AccessController(DB.SQLiteDBContext context)
        {
            db = context;
        }

        // Горизонтальное меню        
        public IActionResult TabMenu()
        {
            return PartialView();
        }
        
        // ПОЛЬЗОВАТЕЛИ ***********************************************************
        // Шапка + разметка для вывода        
        public IActionResult Users()
        {
            return PartialView();
        }

        // Таблица пользователей
        public IActionResult UsersTable()
        {
            // получаем данные пользователя
            var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var users = new List<RKNet_Model.Account.User>();
            
            if(login == "Admin")
            {
                users = db.Users
                .Include(u => u.TTs)
                .Include(u => u.Reports)
                .ToList();
            }
            else
            {
                users = db.Users
                .Include(u => u.TTs)
                .Include(u => u.Reports)
                .Where(u => u.Login != "Admin")
                .ToList();
            }


            
            return PartialView(users);
        }

        // Редактор пользователя
        public IActionResult UserEdit(int userId)
        {
            var userSettings = new UserSettings();

            userSettings.User = db.Users
                .Include(u => u.Roles)
                .Include(u => u.Groups)
                .Include(u => u.TTs)
                .Include(u => u.Reports)
                .FirstOrDefault(u => u.Id == userId);

            if (userSettings.User == null)
            {
                userSettings.User = new RKNet_Model.Account.User();
                userSettings.newUser = true;
            }

            userSettings.Groups = db.Groups.ToList();
            userSettings.Roles = db.Roles.ToList();
            userSettings.TTs = db.TTs.ToList();

            return PartialView(userSettings);
        }

        // Сохранение пользователя
        public IActionResult UserSave(string userjsn)
        {
            var result = new RKNet_Model.Result<string>();

            userjsn = userjsn.Replace("%bkspc%", " ");
            var userJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.user>(userjsn);

            // существующий пользователь
            if (userJsn.id != 0)
            {
                var user = db.Users
                    .Include(u => u.Groups)
                    .Include(u => u.Roles)
                    .Include(u => u.TTs)
                    .Include(u => u.Reports)
                    .FirstOrDefault(u => u.Id == userJsn.id);

                switch (userJsn.attribute)
                {
                    case "userEnabled":
                        user.Enabled = userJsn.enabled;
                        break;

                    case "userAd":
                        user.AdUser = userJsn.ad;
                        break;

                    case "allTT":
                        user.AllTT = userJsn.alltt;
                        // обновляем список точек пользователя
                        if (user.AllTT)
                        {
                            var newTTs = db.TTs.Where(t => !user.TTs.Contains(t)).Where(t => !t.Closed).ToList();
                            user.TTs.AddRange(newTTs);
                        }
                        else
                        {
                            user.TTs = new List<RKNet_Model.TT.TT>();
                        }
                        break;

                    case "userName":
                        if (!string.IsNullOrEmpty(userJsn.name))
                        {
                            user.Name = userJsn.name;
                        }
                        else
                        {
                            result.Ok = false;
                            result.Data = "Имя пользователя заполненно не корректно.";
                            return new ObjectResult(result);
                        }
                        break;

                    case "userLogin":
                        if (!string.IsNullOrEmpty(userJsn.login))
                        {
                            if(user.Login != "Admin")
                            {
                                user.Login = userJsn.login;
                            }                            
                        }
                        else
                        {
                            result.Ok = false;
                            result.Data = "Логин указан не верно.";
                            return new ObjectResult(result);
                        }
                        var userExist = db.Users.FirstOrDefault(u => u.Login == userJsn.login);
                        if (userExist != null)
                        {
                            result.Ok = false;
                            result.Data = "Пользователь с логином " + userJsn.login + " уже существует.";
                            return new ObjectResult(result);
                        }
                        break;

                    case "userPass":
                        if (!string.IsNullOrEmpty(userJsn.password))
                        {
                            var hash = AccountController.SecurePasswordHasher.Hash(userJsn.password.ToLower());
                            user.Password = hash;
                        }
                        else
                        {
                            result.Ok = false;
                            result.Data = "Пароль не может быть пустым.";
                            return new ObjectResult(result);
                        }
                        break;
                    case "userJob":
                        if (!string.IsNullOrEmpty(userJsn.job))
                        {
                            user.JobTitle = userJsn.job;
                        }
                        else
                        {
                            result.Ok = false;
                            result.Data = "Должность указана не верно.";
                            return new ObjectResult(result);
                        }
                        break;

                    case "userMail":
                        if (string.IsNullOrEmpty(userJsn.mail) || new EmailAddressAttribute().IsValid(userJsn.mail))
                        {
                            user.Mail = userJsn.mail;
                        }
                        else
                        {
                            result.Ok = false;
                            result.Data = "Почта пользователя указана не верно. В качестве почты допустимы только действующие e-mail адреса.";
                            return new ObjectResult(result);
                        }
                        break;

                    case "profitFree":
                        user.Reports.ProfitFree = userJsn.profitFree;
                        break;

                    case "profitPro":
                        user.Reports.ProfitPro = userJsn.profitPro;
                        break;

                    case "groups":
                        user.Groups = new List<Group>();

                        foreach (var item in userJsn.items)
                        {
                            var group = db.Groups.FirstOrDefault(g => g.Id == item.id);
                            user.Groups.Add(group);
                        }
                        break;

                    case "roles":
                        user.Roles = new List<Role>();

                        foreach (var item in userJsn.items)
                        {
                            var role = db.Roles.FirstOrDefault(r => r.Id == item.id);
                            user.Roles.Add(role);
                        }
                        break;

                    case "objects":
                        user.TTs = new List<RKNet_Model.TT.TT>();

                        foreach (var item in userJsn.items)
                        {
                            var tt = db.TTs.FirstOrDefault(t => t.Id == item.id);
                            user.TTs.Add(tt);
                        }
                        break;
                }               
                db.Users.Update(user);
            }
            // новый пользователь
            else
            {
                var user = new User();
                var userGroups = new List<Group>();
                var userRoles = new List<Role>();
                var userTTs = new List<RKNet_Model.TT.TT>();

                foreach (var item in userJsn.groups)
                {
                    userGroups.Add(db.Groups.First(g => g.Id == item.id));
                }

                foreach (var item in userJsn.roles)
                {
                    userRoles.Add(db.Roles.First(r => r.Id == item.id));
                }

                foreach (var item in userJsn.objects)
                {
                    userTTs.Add(db.TTs.First(t => t.Id == item.id));
                }

                if (!string.IsNullOrEmpty(userJsn.name))
                {
                    user.Name = userJsn.name;
                }
                else
                {
                    result.Ok = false;
                    result.Data = "Имя пользователя заполненно не корректно.";
                    return new ObjectResult(result);
                }

                if (!string.IsNullOrEmpty(userJsn.login))
                {
                    user.Login = userJsn.login;
                }
                else
                {
                    result.Ok = false;
                    result.Data = "Логин указан не верно.";
                    return new ObjectResult(result);
                }

                var userExist = db.Users.FirstOrDefault(u => u.Login == userJsn.login);
                if(userExist != null)
                {
                    result.Ok = false;
                    result.Data = "Пользователь с логином " + userJsn.login + " уже существует.";
                    return new ObjectResult(result);
                }

                if (!string.IsNullOrEmpty(userJsn.password))
                {
                    var hash = AccountController.SecurePasswordHasher.Hash(userJsn.password.ToLower());
                    user.Password = hash;
                }
                else
                {
                    result.Ok = false;
                    result.Data = "Пароль не может быть пустым.";
                    return new ObjectResult(result);
                }

                if (!string.IsNullOrEmpty(userJsn.job))
                {
                    user.JobTitle = userJsn.job;
                }
                else
                {
                    result.Ok = false;
                    result.Data = "Должность указана не верно.";
                    return new ObjectResult(result);
                }

                if (string.IsNullOrEmpty(userJsn.mail) || new EmailAddressAttribute().IsValid(userJsn.mail))
                {
                    user.Mail = userJsn.mail;
                }
                else
                {
                    result.Ok = false;
                    result.Data = "Почта пользователя указана не верно. В качестве почты допустимы только действующие e-mail адреса.";
                    return new ObjectResult(result);
                }

                // обновляем список точек пользователя
                if (user.AllTT)
                {
                    var newTTs = db.TTs.Where(t => !user.TTs.Contains(t)).Where(t => !t.Closed).ToList();
                    user.TTs.AddRange(newTTs);
                }
                else
                {
                    user.TTs = new List<RKNet_Model.TT.TT>();
                }

                user.Groups = userGroups;
                user.Roles = userRoles;
                user.TTs = userTTs;
                user.Enabled = userJsn.enabled;
                user.AdUser = userJsn.ad;
                user.Reports.ProfitFree = userJsn.profitFree;
                user.Reports.ProfitPro = userJsn.profitPro;
                user.AllTT = userJsn.alltt;

                db.Users.Add(user);
            }

            db.SaveChanges();

            return new ObjectResult(result);
        }

        // Удаление пользователя
        public IActionResult UserDelete(int userId)
        {
            try
            {
                var user = db.Users
                    .Include(u => u.Roles)
                    .Include(u => u.Groups)
                    .Include(u => u.TTs)
                    .Include(u => u.Reports)
                    .FirstOrDefault(u => u.Id == userId);

                if (user != null)
                {
                    db.Users.Remove(user);
                    db.SaveChanges();
                }
                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Выбранные элементы коллекций на пользователе
        public IActionResult GetUserItems(int userId, string selectId)
        {
            var user = db.Users
                .Include(u => u.Roles)
                .Include(u => u.Groups)
                .Include(u => u.TTs)
                .FirstOrDefault(u => u.Id == userId);

            switch (selectId)
            {
                case "roles":
                    return new ObjectResult(user.Roles);
                case "groups":
                    return new ObjectResult(user.Groups);
                case "objects":
                    return new ObjectResult(user.TTs);
                default:
                    return new ObjectResult("empty");
            }
        }

        // ГРУППЫ ***********************************************************
        // Шапка + разметка для вывода
        public IActionResult Groups()
        {
            return PartialView();
        }

        // Таблица групп
        public IActionResult GroupsTable()
        {
            var groups = db.Groups.Include(g => g.Users).ToList();
            return PartialView(groups);
        }

        // Редактор группы
        public IActionResult GroupEdit(int groupId)
        {
            var groupSettings = new GroupSettings();

            groupSettings.Group = db.Groups
                .Include(g => g.Users)
                .Include(g => g.Roles)
                .FirstOrDefault(g => g.Id == groupId);

            if (groupSettings.Group == null)
            {
                groupSettings.Group = new Group();
                groupSettings.newGroup = true;
            }

            groupSettings.Users = db.Users.ToList();
            groupSettings.Roles = db.Roles.ToList();

            return PartialView(groupSettings);
        }        

        // Сохранение группы
        public IActionResult GroupSave(string groupjsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {

                groupjsn = groupjsn.Replace("%bkspc%", " ");
                var groupJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.group>(groupjsn);

                // существующая группа
                if (groupJsn.id != 0)
                {
                    var group = db.Groups
                        .Include(g => g.Users)
                        .Include(g => g.Roles)
                        .FirstOrDefault(g => g.Id == groupJsn.id);

                    switch (groupJsn.attribute)
                    {
                        case "groupName":
                            if (!string.IsNullOrEmpty(groupJsn.name))
                            {
                                group.Name = groupJsn.name;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Название группы заполненно не корректно.";
                                return new ObjectResult(result);
                            }
                            var existName = db.Groups.FirstOrDefault(g => g.Name == group.Name);
                            if (existName != null)
                            {
                                result.Ok = false;
                                result.Data = "Группа с названием \"" + existName.Name + "\" уже существует, введите другое имя группы.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "groupDescription":
                            if (!string.IsNullOrEmpty(groupJsn.description))
                            {
                                group.Description = groupJsn.description;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Необходимо заполнить описание группы: назначение, обощенные права на группе, категория пользователей и т.п.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "users":
                            group.Users = new List<User>();
                            foreach (var item in groupJsn.items)
                            {
                                var user = db.Users.FirstOrDefault(u => u.Id == item.id);
                                group.Users.Add(user);
                            }
                            break;

                        case "roles":
                            group.Roles = new List<Role>();

                            foreach (var item in groupJsn.items)
                            {
                                var role = db.Roles.FirstOrDefault(r => r.Id == item.id);
                                group.Roles.Add(role);
                            }
                            break;
                    }


                    db.Groups.Update(group);
                }
                // новая группа
                else
                {
                    var group = new Group();
                    var groupUsers = new List<User>();
                    var groupRoles = new List<Role>();

                    foreach (var item in groupJsn.users)
                    {
                        groupUsers.Add(db.Users.First(u => u.Id == item.id));
                    }

                    foreach (var item in groupJsn.roles)
                    {
                        groupRoles.Add(db.Roles.First(r => r.Id == item.id));
                    }

                    if (!string.IsNullOrEmpty(groupJsn.name))
                    {
                        group.Name = groupJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название группы заполненно не корректно.";
                        return new ObjectResult(result);
                    }

                    var existName = db.Groups.FirstOrDefault(g => g.Name == group.Name);
                    if(existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Группа с названием \"" + existName.Name + "\" уже существует, введите другое имя группы.";
                        return new ObjectResult(result);
                    }

                    if (!string.IsNullOrEmpty(groupJsn.description))
                    {
                        group.Description = groupJsn.description;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Необходимо заполнить описание группы: назначение, обощенные права на группе, категория пользователей и т.п.";
                        return new ObjectResult(result);
                    }

                    group.Users = groupUsers;
                    group.Roles = groupRoles;

                    db.Groups.Add(group);
                }


                db.SaveChanges();

            }
            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.ToString();
            }

            return new ObjectResult(result);
        }        

        // Удаление группы
        public IActionResult GroupDelete(int groupId)
        {
            try
            {
                var group = db.Groups
                    .Include(g => g.Users)
                    .Include(g => g.Roles)
                    .FirstOrDefault(g => g.Id == groupId);

                db.Groups.Remove(group);
                db.SaveChanges();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Выбранные элементы коллекций на группе
        public IActionResult GetGroupItems(int groupId, string selectId)
        {
            var group = db.Groups
                .Include(g => g.Users)
                .Include(g => g.Roles)
                .FirstOrDefault(g => g.Id == groupId);

            switch (selectId)
            {
                case "users":
                    return new ObjectResult(group.Users);
                case "roles":
                    return new ObjectResult(group.Roles);
                default:
                    return new ObjectResult("empty");
            }
        }

        // Id группы по имени
        public IActionResult GetGroupId(string groupName)
        {
            var groupId = db.Groups.FirstOrDefault(g => g.Name == groupName).Id;
            return new ObjectResult(groupId);
        }

        // РОЛИ ***********************************************************
        // Шапка + разметка для вывода
        public IActionResult Roles()
        {
            return PartialView();
        }

        // Таблица ролей
        public IActionResult RolesTable()
        {
            var roles = db.Roles.Include(r => r.Users).Include(r => r.Groups).ToList();
            return PartialView(roles);
        }

        // Редактор роли
        public IActionResult RoleEdit(int roleId)
        {
            var roleSettings = new RoleSettings();

            roleSettings.Role = db.Roles
                .Include(r => r.Users)
                .Include(r => r.Groups)
                .FirstOrDefault(r => r.Id == roleId);

            if (roleSettings.Role == null)
            {
                roleSettings.Role = new Role();
                roleSettings.newRole = true;
            }

            roleSettings.Users = db.Users.ToList();
            roleSettings.Groups = db.Groups.ToList();

            return PartialView(roleSettings);
        }

        // Сохранение роли
        public IActionResult RoleSave(string rolejsn)
        {
            var result = new RKNet_Model.Result<string>();
            try
            {                
                rolejsn = rolejsn.Replace("%bkspc%", " ");
                var roleJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.role>(rolejsn);

                // существующая роль
                if (roleJsn.id != 0)
                {
                    var role = db.Roles
                        .Include(r => r.Users)
                        .Include(r => r.Groups)
                        .FirstOrDefault(r => r.Id == roleJsn.id);                    

                    switch (roleJsn.attribute)
                    {
                        case "roleName":
                            if (!string.IsNullOrEmpty(roleJsn.name))
                            {
                                role.Name = roleJsn.name;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Название роли заполненно некорректно.";
                                return new ObjectResult(result);
                            }

                            var existName = db.Roles.FirstOrDefault(r => r.Name == role.Name);                            
                            if (existName != null)
                            {
                                result.Ok = false;
                                result.Data = "Роль с названием \"" + existName.Name + "\" уже существует, введите другое имя.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "roleCode":
                            if (!string.IsNullOrEmpty(roleJsn.code))
                            {
                                role.Code = roleJsn.code;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Код роли заполнен некорректно.";
                                return new ObjectResult(result);
                            }
                            var existCode = db.Roles.FirstOrDefault(r => r.Code == role.Code);
                            if (existCode != null)
                            {
                                result.Ok = false;
                                result.Data = "Код роли \"" + existCode.Code + "\" уже существует, введите другой код";
                                return new ObjectResult(result);
                            }
                            break;

                        case "users":
                            role.Users = new List<User>();
                            foreach (var item in roleJsn.items)
                            {
                                var user = db.Users.FirstOrDefault(u => u.Id == item.id);
                                role.Users.Add(user);
                            }
                            break;

                        case "groups":
                            role.Groups = new List<Group>();

                            foreach (var item in roleJsn.items)
                            {
                                var group = db.Groups.FirstOrDefault(g => g.Id == item.id);
                                role.Groups.Add(group);
                            }
                            break;
                    }


                    db.Roles.Update(role);
                }
                // новая роль
                else
                {
                    var role = new Role();
                    var roleUsers = new List<User>();
                    var roleGroups = new List<Group>();

                    foreach (var item in roleJsn.users)
                    {
                        roleUsers.Add(db.Users.First(u => u.Id == item.id));
                    }

                    foreach (var item in roleJsn.groups)
                    {
                        roleGroups.Add(db.Groups.First(g => g.Id == item.id));
                    }

                    if (!string.IsNullOrEmpty(roleJsn.name))
                    {
                        role.Name = roleJsn.name;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Название роли заполненно некорректно.";
                        return new ObjectResult(result);
                    }

                    var existName = db.Roles.FirstOrDefault(r => r.Name == role.Name);
                    if (existName != null)
                    {
                        result.Ok = false;
                        result.Data = "Роль с названием \"" + existName.Name + "\" уже существует, введите другое имя.";
                        return new ObjectResult(result);
                    }

                    if (!string.IsNullOrEmpty(roleJsn.code))
                    {
                        role.Code = roleJsn.code;
                    }
                    else
                    {
                        result.Ok = false;
                        result.Data = "Код роли заполнен некорректно.";
                        return new ObjectResult(result);
                    }

                    var existCode = db.Roles.FirstOrDefault(r => r.Code == role.Code);
                    if (existCode != null)
                    {
                        result.Ok = false;
                        result.Data = "Код роли \"" + existCode.Code + "\" уже существует, введите другой код";
                        return new ObjectResult(result);
                    }

                    role.Users = roleUsers;
                    role.Groups = roleGroups;

                    db.Roles.Add(role);
                }


                db.SaveChanges();

            }
            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.ToString();
            }

            return new ObjectResult(result);
        }

        // Удаление роли
        public IActionResult RoleDelete(int roleId)
        {
            try
            {
                var role = db.Roles
                    .Include(r => r.Users)
                    .Include(r => r.Groups)
                    .FirstOrDefault(r => r.Id == roleId);

                db.Roles.Remove(role);
                db.SaveChanges();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Выбранные элементы коллекций на роли
        public IActionResult GetRoleItems(int roleId, string selectId)
        {
            var role = db.Roles
                .Include(r => r.Users)
                .Include(r => r.Groups)
                .FirstOrDefault(r => r.Id == roleId);

            switch (selectId)
            {
                case "users":
                    return new ObjectResult(role.Users);
                case "groups":
                    return new ObjectResult(role.Groups);
                default:
                    return new ObjectResult("empty");
            }
        }

        // Id роли по имени
        public IActionResult GetRoleId(string roleName)
        {
            var roleId = db.Roles.FirstOrDefault(r => r.Name == roleName).Id;
            return new ObjectResult(roleId);
        }
        
    }
}
