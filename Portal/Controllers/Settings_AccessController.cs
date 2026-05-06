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
using RKNet_Model.Reports;
using RKNet_Model.TT;
using Portal.Global;

namespace Portal.Controllers
{
    [Authorize(Roles = "settings, HR")]
    public class Settings_AccessController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        public Settings_AccessController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
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

            if (login == "Admin")
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
                .Include(u => u.Groups)
                .Include(u => u.Reports)
                .Where(u => u.Login != "Admin")
                .ToList();

                

                if(User.IsInRole("HR"))
                {
                    var group = db.Groups.FirstOrDefault(x => x.Id == 22);
                    users = users.Where(x => x.Groups.Contains(group)).ToList(); 
                }

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
            var nxWarnings = new List<string>();
            userjsn = userjsn.Replace("%bkspc%", " ");
            var userJsn = JsonConvert.DeserializeObject<ViewModels.Settings_Access.json.user>(userjsn);

            try
            {
                if (userJsn.id != 0)
                {
                    var user = db.Users
                        .Include(u => u.Groups)
                        .Include(u => u.Roles)
                        .Include(u => u.TTs)
                        .Include(u => u.Reports)
                        .FirstOrDefault(u => u.Id == userJsn.id);

                    if (user == null)
                    {
                        result.Ok = false;
                        result.Data = "Пользователь не найден.";
                        return new ObjectResult(result);
                    }

                    var hadNxRoleBefore = HasNxRole(user);

                    switch (userJsn.attribute)
                    {
                        case "userEnabled":
                            user.Enabled = userJsn.enabled;
                            nxWarnings = SyncNxStateForEnabledUser(user);
                            break;

                        case "userAd":
                            user.AdUser = userJsn.ad;
                            break;

                        case "allTT":
                            user.AllTT = userJsn.alltt;
                            if (user.AllTT)
                            {
                                var newTTs = db.TTs.Where(t => !user.TTs.Contains(t) && !t.Closed).ToList();
                                user.TTs.AddRange(newTTs);
                            }
                            else
                            {
                                user.TTs.Clear();
                            }
                            nxWarnings = SyncNxGroupsIfAllowed(user, hadNxRoleBefore);
                            break;

                        case "userName":
                            if (!string.IsNullOrEmpty(userJsn.name))
                            {
                                user.Name = userJsn.name;
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Имя пользователя заполнено некорректно.";
                                return new ObjectResult(result);
                            }
                            break;

                        case "userLogin":
                            if (!string.IsNullOrEmpty(userJsn.login))
                            {
                                if (user.Login != "Admin")
                                {
                                    user.Login = userJsn.login;
                                }
                                else
                                {
                                    result.Ok = false;
                                    result.Data = "Логин администратора не может быть изменен.";
                                    return new ObjectResult(result);
                                }

                                var userExist = db.Users.FirstOrDefault(u => u.Login == userJsn.login);
                                if (userExist != null)
                                {
                                    result.Ok = false;
                                    result.Data = "Пользователь с логином " + userJsn.login + " уже существует.";
                                    return new ObjectResult(result);
                                }
                            }
                            else
                            {
                                result.Ok = false;
                                result.Data = "Логин пользователя заполнен некорректно.";
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
                                result.Data = "Пароль пользователя заполнен некорректно.";
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
                                result.Data = "Должность пользователя заполнена некорректно.";
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
                                result.Data = "Адрес электронной почты заполнен некорректно.";
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
                            user.Groups.Clear();
                            foreach (var item in userJsn.items)
                            {
                                var group = db.Groups.FirstOrDefault(g => g.Id == item.id);
                                if (group != null)
                                {
                                    user.Groups.Add(group);
                                }
                            }
                            nxWarnings = SyncNxGroupsIfAllowed(user, hadNxRoleBefore);
                            break;

                        case "roles":
                            user.Roles.Clear();
                            foreach (var item in userJsn.items)
                            {
                                var role = db.Roles.FirstOrDefault(r => r.Id == item.id);
                                if (role != null)
                                {
                                    user.Roles.Add(role);
                                }
                            }
                            nxWarnings = SyncNxGroupsIfAllowed(user, hadNxRoleBefore);
                            break;

                        case "objects":
                            user.TTs.Clear();
                            foreach (var item in userJsn.items)
                            {
                                var tt = db.TTs.FirstOrDefault(t => t.Id == item.id);
                                if (tt != null)
                                {
                                    user.TTs.Add(tt);
                                }
                            }
                            nxWarnings = SyncNxGroupsIfAllowed(user, hadNxRoleBefore);
                            break;
                    }

                    db.Users.Update(user);
                }
                else
                {
                    var user = new User
                    {
                        Name = !string.IsNullOrEmpty(userJsn.name) ? userJsn.name : throw new Exception("Имя пользователя заполнено некорректно."),
                        Login = !string.IsNullOrEmpty(userJsn.login) ? userJsn.login : throw new Exception("Логин пользователя заполнен некорректно."),
                        Password = !string.IsNullOrEmpty(userJsn.password) ? AccountController.SecurePasswordHasher.Hash(userJsn.password.ToLower()) : throw new Exception("Пароль пользователя заполнен некорректно."),
                        JobTitle = !string.IsNullOrEmpty(userJsn.job) ? userJsn.job : throw new Exception("Должность пользователя заполнена некорректно."),
                        Mail = (string.IsNullOrEmpty(userJsn.mail) || new EmailAddressAttribute().IsValid(userJsn.mail)) ? userJsn.mail : throw new Exception("Адрес электронной почты заполнен некорректно."),
                        Enabled = userJsn.enabled,
                        AdUser = userJsn.ad,
                        Reports = new UserReport
                        {
                            ProfitFree = userJsn.profitFree,
                            ProfitPro = userJsn.profitPro
                        },
                        AllTT = userJsn.alltt,
                        Groups = userJsn.groups.Select(item => db.Groups.FirstOrDefault(g => g.Id == item.id)).ToList(),
                        Roles = userJsn.roles.Select(item => db.Roles.FirstOrDefault(r => r.Id == item.id)).ToList(),
                        TTs = userJsn.objects.Select(item => db.TTs.FirstOrDefault(t => t.Id == item.id)).ToList()
                    };

                    nxWarnings = SyncNxGroupsIfAllowed(user, false);
                    db.Users.Add(user);
                }

                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                result.Ok = false;
                result.Data = "Ошибка параллелизма при сохранении данных. Попробуйте еще раз.";
                return UserSaveResult(result, nxWarnings);
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Data = ex.Message;
                return UserSaveResult(result, nxWarnings);
            }

            return UserSaveResult(result, nxWarnings);
        }

        private ObjectResult UserSaveResult(RKNet_Model.Result<string> result, List<string> nxWarnings)
        {
            return new ObjectResult(new
            {
                ok = result.Ok,
                data = result.Data,
                errorMessage = result.ErrorMessage,
                warnings = nxWarnings ?? new List<string>()
            });
        }

        private List<string> SyncNxGroupsForUser(string login, IEnumerable<TT> userTTs, bool allTT)
        {
            var warnings = new List<string>();
            var nxLayoutRequiredLocationTypes = new HashSet<Guid>
            {
                Guid.Parse("94AD659C-AF5B-4CA0-50AD-08DBDF6ABE84"),
                Guid.Parse("B0E427F9-8996-4C03-33C1-08DBDF713401")
            };

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new Exception("Логин пользователя пустой. Невозможно синхронизировать группы NX.");
            }

            var tts = userTTs?
                .Where(tt => tt != null)
                .ToList() ?? new List<TT>();

            var nxClient = new NxRestClient();
            var nxGroups = nxClient.GetUserGroups();
            var allTtGroup = nxGroups.FirstOrDefault(group =>
                string.Equals(group.Name, "ALLTT", StringComparison.OrdinalIgnoreCase));

            var managedNxGroupIds = dbSql.Locations
                .Where(location => location.NXLayout != null)
                .Select(location => location.NXLayout.Value)
                .Distinct()
                .ToList();

            var requiredNxGroupIds = new List<Guid>();

            if (allTtGroup != null && Guid.TryParse(allTtGroup.Id, out var allTtGroupId))
            {
                managedNxGroupIds.Add(allTtGroupId);
            }

            managedNxGroupIds = managedNxGroupIds
                .Distinct()
                .ToList();

            if (allTT)
            {
                if (allTtGroup == null || !Guid.TryParse(allTtGroup.Id, out var parsedAllTtGroupId))
                {
                    throw new Exception("В NX не найдена группа ALLTT. Изменения не применены.");
                }

                requiredNxGroupIds.Add(parsedAllTtGroupId);
            }
            else
            {
                foreach (var tt in tts)
                {
                    var restaurantSifr = tt.Restaurant_Sifr;

                    var location = dbSql.Locations
                        .Include(item => item.LocationType)
                        .FirstOrDefault(item => item.RKCode == restaurantSifr);

                    if (location == null)
                    {
                        throw new Exception($"Для ТТ {tt.Name} с Restaurant_Sifr {restaurantSifr} не найден Location. Изменения не применены.");
                    }

                    if (location.NXLayout == null)
                    {
                        var locationTypeGuid = location.LocationType?.Guid;
                        var shouldThrowNxLayoutError =
                            location.Actual == 1  &&
                            locationTypeGuid.HasValue &&
                            nxLayoutRequiredLocationTypes.Contains(locationTypeGuid.Value);

                        if (shouldThrowNxLayoutError)
                        {
                            throw new Exception($"Для ТТ {tt.Name} не найден NXLayout. Изменения не применены.");
                        }

                        continue;
                    }

                    requiredNxGroupIds.Add(location.NXLayout.Value);
                }
            }

            requiredNxGroupIds = requiredNxGroupIds
                .Distinct()
                .ToList();

            nxClient.SyncUserGroups(login, requiredNxGroupIds, managedNxGroupIds);

            return warnings;
        }

        private List<string> SyncNxStateForEnabledUser(User user)
        {
            if (user == null)
            {
                return new List<string>();
            }

            if (string.IsNullOrWhiteSpace(user.Login))
            {
                throw new Exception("Логин пользователя пустой. Невозможно синхронизировать NX.");
            }

            var nxClient = new NxRestClient();
            var nxUsers = nxClient.GetUsers();

            if (!user.Enabled)
            {
                if (!nxClient.UserExists(user.Login, nxUsers))
                {
                    return new List<string>();
                }

                return ExecuteNxUserSyncWithWarnings(user.Login, () => ClearManagedNxGroupsForUser(user.Login));
            }

            if (!nxClient.UserExists(user.Login, nxUsers))
            {
                return new List<string> { GetNxUserNotFoundWarning(user.Login) };
            }

            if (HasNxRole(user))
            {
                return ExecuteNxUserSyncWithWarnings(user.Login, () => SyncNxGroupsForUser(user.Login, user.TTs?.ToList(), user.AllTT));
            }

            return ExecuteNxUserSyncWithWarnings(user.Login, () => ClearManagedNxGroupsForUser(user.Login));
        }

        private List<string> SyncNxGroupsIfAllowed(User user, bool hadNxRoleBefore)
        {
            if (user != null && !user.Enabled)
            {
                return ExecuteNxUserSyncWithWarnings(user.Login, () => ClearManagedNxGroupsForUser(user.Login));
            }

            var hasNxRoleNow = HasNxRole(user);

            if (hasNxRoleNow)
            {
                var nxClient = new NxRestClient();
                if (!nxClient.UserExists(user.Login, nxClient.GetUsers()))
                {
                    return new List<string> { GetNxUserNotFoundWarning(user.Login) };
                }

                return ExecuteNxUserSyncWithWarnings(user.Login, () => SyncNxGroupsForUser(user.Login, user.TTs?.ToList(), user.AllTT));
            }

            if (hadNxRoleBefore || !string.IsNullOrWhiteSpace(user?.Login))
            {
                return ExecuteNxUserSyncWithWarnings(user.Login, () => ClearManagedNxGroupsForUser(user.Login));
            }

            return new List<string>();
        }

        private List<string> ExecuteNxUserSyncWithWarnings(string login, Func<List<string>> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (IsNxAdministratorRightsForbidden(ex))
            {
                return new List<string> { GetNxAdminRightsWarning(login) };
            }
        }

        private bool IsNxAdministratorRightsForbidden(Exception ex)
        {
            var message = ex?.ToString() ?? string.Empty;
            return message.IndexOf("is not permitted to modify User", StringComparison.OrdinalIgnoreCase) >= 0
                && message.IndexOf("administrator", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetNxUserNotFoundWarning(string login)
        {
            return $"Пользователь '{login}' не найден в NX, изменения применены только для портала.";
        }

        private string GetNxAdminRightsWarning(string login)
        {
            return $"Пользователь '{login}' имеет администраторские права в NX, изменения применены только для портала.";
        }

        private List<string> ClearManagedNxGroupsForUser(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new Exception("Логин пользователя пустой. Невозможно очистить группы NX.");
            }

            var nxClient = new NxRestClient();
            var nxUsers = nxClient.GetUsers();

            if (!nxClient.UserExists(login, nxUsers))
            {
                return new List<string>();
            }

            var managedNxGroupIds = GetManagedNxGroupIds(nxClient.GetUserGroups());
            nxClient.SyncUserGroups(login, Array.Empty<Guid>(), managedNxGroupIds);

            return new List<string>();
        }

        private bool HasNxRole(User user)
        {
            if (user == null)
            {
                return false;
            }

            const string nxRoleName = "NX";

            var hasDirectNxRole = (user.Roles ?? new List<Role>())
                .Where(role => role != null)
                .Any(role => string.Equals(role.Name, nxRoleName, StringComparison.OrdinalIgnoreCase));

            if (hasDirectNxRole)
            {
                return true;
            }

            var groupIds = (user.Groups ?? new List<Group>())
                .Where(group => group != null)
                .Select(group => group.Id)
                .Distinct()
                .ToList();

            if (!groupIds.Any())
            {
                return false;
            }

            var nxRoleNameLower = nxRoleName.ToLower();

            return db.Groups
                .Include(group => group.Roles)
                .Any(group =>
                    groupIds.Contains(group.Id) &&
                    group.Roles.Any(role => role.Name != null && role.Name.ToLower() == nxRoleNameLower));
        }

        private List<Guid> GetManagedNxGroupIds(IEnumerable<NxRestClient.NxItemInfo> nxGroups)
        {
            var managedNxGroupIds = dbSql.Locations
                .Where(location => location.NXLayout != null)
                .Select(location => location.NXLayout.Value)
                .Distinct()
                .ToList();

            var allTtGroup = nxGroups.FirstOrDefault(group =>
                string.Equals(group.Name, "ALLTT", StringComparison.OrdinalIgnoreCase));

            if (allTtGroup != null && Guid.TryParse(allTtGroup.Id, out var allTtGroupId))
            {
                managedNxGroupIds.Add(allTtGroupId);
            }

            return managedNxGroupIds
                .Distinct()
                .ToList();
        }

        // Удаление пользователя
        [HttpPost]
        public IActionResult MigratePortalUsersToNxGroups()
        {
            var result = new RKNet_Model.Result<object>();
            var nxClient = new NxRestClient();
            User currentUser = null;
            NxMigrationAction? currentAction = null;
            var skippedUsers403 = new List<object>();

            try
            {
                var users = db.Users
                    .Include(u => u.TTs)
                    .Include(u => u.Roles)
                    .Include(u => u.Groups)
                    .Where(u => !string.IsNullOrWhiteSpace(u.Login))
                    .Where(u => !string.Equals(u.Login, "VVAbramov"))
                    .OrderBy(u => u.Login)
                    .ToList();

                var nxGroups = nxClient.GetUserGroups();
                var nxUsers = nxClient.GetUsers();
                var plans = users
                    .Select(user => new
                    {
                        User = user,
                        user.Login,
                        Plan = BuildNxMigrationPlan(user, nxGroups, nxUsers)
                    })
                    .ToList();

                foreach (var item in plans)
                {
                    currentUser = item.User;
                    currentAction = item.Plan.Action;

                    try
                    {
                        if (item.Plan.Action == NxMigrationAction.Skip)
                        {
                            continue;
                        }

                        if (item.Plan.Action == NxMigrationAction.ClearManagedGroups)
                        {
                            nxClient.SyncUserGroups(item.Login, Array.Empty<Guid>(), item.Plan.ManagedNxGroupIds);
                            continue;
                        }

                        nxClient.SyncUserGroups(item.Login, item.Plan.RequiredNxGroupIds, item.Plan.ManagedNxGroupIds);
                    }
                    catch (Exception ex) when (IsNxForbiddenError(ex))
                    {
                        skippedUsers403.Add(new
                        {
                            login = item.Login,
                            name = item.User?.Name,
                            action = item.Plan.Action.ToString(),
                            error = ex.Message
                        });
                    }
                }

                result.Ok = true;
                result.Data = new
                {
                    migratedUsers = plans.Count,
                    logins = plans.Select(item => item.Login).ToList(),
                    skippedUsers403
                };
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Data = BuildNxMigrationErrorMessage(ex, currentUser, currentAction);
            }

            return new ObjectResult(result);
        }

        private bool IsNxForbiddenError(Exception ex)
        {
            return ex != null &&
                ex.ToString().IndexOf("403", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string BuildNxMigrationErrorMessage(Exception ex, User currentUser, NxMigrationAction? currentAction)
        {
            if (currentUser == null)
            {
                return ex.ToString();
            }

            var ttNames = (currentUser.TTs ?? new List<TT>())
                .Where(tt => tt != null)
                .Select(tt => tt.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();

            var ttInfo = ttNames.Any()
                ? string.Join(", ", ttNames)
                : "нет ТТ";

            return
                $"{ex}\n" +
                $"Migration user login: {currentUser.Login}\n" +
                $"Migration user name: {currentUser.Name}\n" +
                $"Migration action: {(currentAction?.ToString() ?? "unknown")}\n" +
                $"Migration user TTs: {ttInfo}";
        }

        private NxMigrationPlan BuildNxMigrationPlan(
            User user,
            IEnumerable<NxRestClient.NxItemInfo> nxGroups,
            IEnumerable<NxRestClient.NxUserInfo> nxUsers)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Login))
            {
                return new NxMigrationPlan
                {
                    Action = NxMigrationAction.Skip,
                    ManagedNxGroupIds = new List<Guid>(),
                    RequiredNxGroupIds = new List<Guid>()
                };
            }

            var managedNxGroupIds = GetManagedNxGroupIds(nxGroups);
            var hasNxAccount = new NxRestClient().UserExists(user.Login, nxUsers);

            if (!user.Enabled)
            {
                return new NxMigrationPlan
                {
                    Action = hasNxAccount ? NxMigrationAction.ClearManagedGroups : NxMigrationAction.Skip,
                    ManagedNxGroupIds = managedNxGroupIds,
                    RequiredNxGroupIds = new List<Guid>()
                };
            }

            if (!HasNxRole(user))
            {
                return new NxMigrationPlan
                {
                    Action = hasNxAccount ? NxMigrationAction.ClearManagedGroups : NxMigrationAction.Skip,
                    ManagedNxGroupIds = managedNxGroupIds,
                    RequiredNxGroupIds = new List<Guid>()
                };
            }

            if (!hasNxAccount)
            {
                return new NxMigrationPlan
                {
                    Action = NxMigrationAction.Skip,
                    ManagedNxGroupIds = managedNxGroupIds,
                    RequiredNxGroupIds = new List<Guid>()
                };
            }

            var syncPlan = BuildNxGroupSyncPlan(user.Login, user.TTs?.ToList(), user.AllTT, nxGroups);
            return new NxMigrationPlan
            {
                Action = NxMigrationAction.SyncGroups,
                ManagedNxGroupIds = syncPlan.ManagedNxGroupIds,
                RequiredNxGroupIds = syncPlan.RequiredNxGroupIds
            };
        }

        [HttpPost]
        public IActionResult CheckPortalUsersInNx()
        {
            var result = new RKNet_Model.Result<object>();

            try
            {
                var nxClient = new NxRestClient();
                var nxUsers = nxClient.GetUsers();
                var portalUsers = db.Users
                    .Where(u => !string.IsNullOrWhiteSpace(u.Login) && u.Enabled == true)
                    .Select(u => new
                    {
                        u.Id,
                        u.Login
                    })
                    .OrderBy(u => u.Login)
                    .ToList();

                var missingUsers = portalUsers
                    .Where(user => !nxClient.UserExists(user.Login, nxUsers))
                    .Select(user => new
                    {
                        user.Id,
                        user.Login
                    })
                    .ToList();

                Console.WriteLine("NX user check started.");
                foreach (var user in missingUsers)
                {
                    Console.WriteLine($"NX user not found. Portal userId: {user.Id}, login: {user.Login}");
                }
                Console.WriteLine($"NX user check finished. Missing users: {missingUsers.Count}");

                result.Ok = true;
                result.Data = new
                {
                    totalPortalUsers = portalUsers.Count,
                    missingUsersCount = missingUsers.Count,
                    missingUsers
                };
            }
            catch (Exception ex)
            {
                result.Ok = false;
                result.Data = ex.ToString();
            }

            return new ObjectResult(result);
        }

        private NxGroupSyncPlan BuildNxGroupSyncPlan(
            string login,
            IEnumerable<TT> userTTs,
            bool allTT,
            IEnumerable<NxRestClient.NxItemInfo> nxGroups)
        {
            var nxLayoutRequiredLocationTypes = new HashSet<Guid>
            {
                Guid.Parse("94AD659C-AF5B-4CA0-50AD-08DBDF6ABE84"),
                Guid.Parse("B0E427F9-8996-4C03-33C1-08DBDF713401")
            };

            if (string.IsNullOrWhiteSpace(login))
            {
                throw new Exception("Логин пользователя пустой. Невозможно синхронизировать группы NX.");
            }

            var tts = userTTs?
                .Where(tt => tt != null)
                .ToList() ?? new List<TT>();

            var allTtGroup = nxGroups.FirstOrDefault(group =>
                string.Equals(group.Name, "ALLTT", StringComparison.OrdinalIgnoreCase));

            var managedNxGroupIds = dbSql.Locations
                .Where(location => location.NXLayout != null)
                .Select(location => location.NXLayout.Value)
                .Distinct()
                .ToList();

            var requiredNxGroupIds = new List<Guid>();

            if (allTtGroup != null && Guid.TryParse(allTtGroup.Id, out var allTtGroupId))
            {
                managedNxGroupIds.Add(allTtGroupId);
            }

            managedNxGroupIds = managedNxGroupIds
                .Distinct()
                .ToList();

            if (allTT)
            {
                if (allTtGroup == null || !Guid.TryParse(allTtGroup.Id, out var parsedAllTtGroupId))
                {
                    throw new Exception("В NX не найдена группа ALLTT. Изменения не применены.");
                }

                requiredNxGroupIds.Add(parsedAllTtGroupId);
            }
            else
            {
                foreach (var tt in tts)
                {
                    var location = dbSql.Locations
                        .Include(item => item.LocationType)
                        .FirstOrDefault(item => item.RKCode == tt.Restaurant_Sifr);

                    if (location == null)
                    {
                        throw new Exception($"Для ТТ {tt.Name} с Restaurant_Sifr {tt.Restaurant_Sifr} не найден Location. Изменения не применены.");
                    }

                    if (location.NXLayout == null)
                    {
                        var locationTypeGuid = location.LocationType?.Guid;
                        var shouldThrowNxLayoutError =
                            location.Actual == 1 &&
                            locationTypeGuid.HasValue &&
                            nxLayoutRequiredLocationTypes.Contains(locationTypeGuid.Value);

                        if (shouldThrowNxLayoutError)
                        {
                            throw new Exception($"Для ТТ {tt.Name} не найден NXLayout. Изменения не применены.");
                        }

                        continue;
                    }

                    requiredNxGroupIds.Add(location.NXLayout.Value);
                }
            }

            return new NxGroupSyncPlan
            {
                RequiredNxGroupIds = requiredNxGroupIds.Distinct().ToList(),
                ManagedNxGroupIds = managedNxGroupIds
            };
        }

        private class NxGroupSyncPlan
        {
            public List<Guid> RequiredNxGroupIds { get; set; }
            public List<Guid> ManagedNxGroupIds { get; set; }
        }

        private class NxMigrationPlan
        {
            public NxMigrationAction Action { get; set; }
            public List<Guid> RequiredNxGroupIds { get; set; }
            public List<Guid> ManagedNxGroupIds { get; set; }
        }

        private enum NxMigrationAction
        {
            Skip,
            ClearManagedGroups,
            SyncGroups
        }

        public IActionResult UserDelete(int userId)
        {
            try
            {
                // Получаем пользователя с его связями
                var user = db.Users
                    .Include(u => u.Roles)
                    .Include(u => u.Groups)
                    .Include(u => u.TTs)
                    .Include(u => u.Reports)
                    .SingleOrDefault(u => u.Id == userId);

                if (user != null)
                {
                    db.Users.Remove(user);
                    db.SaveChanges();
                }
                else
                {
                    return NotFound("User not found.");
                }
                return Ok("User deleted successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Логирование ошибки параллелизма
                return Conflict("Concurrency error occurred: " + ex.Message);
            }
            catch (Exception e)
            {
                return BadRequest("Error: " + e.Message);
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

                // Существующая группа
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
                                result.Data = "Название группы заполнено некорректно.";
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
                                result.Data = "Необходимо заполнить описание группы: назначение, общие права на группе, категория пользователей и т.п.";
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
                // Новая группа
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
                        result.Data = "Название группы заполнено не корректно.";
                        return new ObjectResult(result);
                    }

                    var existName = db.Groups.FirstOrDefault(g => g.Name == group.Name);
                    if (existName != null)
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
                        result.Data = "Необходимо заполнить описание группы: назначение, общие права на группе, категория пользователей и т.п.";
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
                                result.Data = "Название роли заполнено некорректно.";
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
                        result.Data = "Название роли заполнено некорректно.";
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
