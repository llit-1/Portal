using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Portal.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.DirectoryServices;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;



namespace Portal.Controllers
{
    public class AccountController : Controller
    {
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext dbSql;
        public AccountController(DB.SQLiteDBContext context, DB.MSSQLDBContext dbSqlContext)
        {
            db = context;
            dbSql = dbSqlContext;
        }

        // Логин - ввод данных    
        [HttpGet]
        public IActionResult Login()
        {
            var model = new LoginViewModel();
            return View(model);
        }

        // Логин - обработка данных
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {      
            try
            {
                if (ModelState.IsValid)
                {
                    var login = model.login.ToLower();
                    var password = model.password.ToLower();

                    if (!string.IsNullOrEmpty(model.userIp))
                    {
                        HttpContext.Session.SetString("ip", model.userIp);
                    } 
                    else
                    {
                        HttpContext.Session.SetString("ip", HttpContext.Connection.RemoteIpAddress.ToString());
                    }

                    

                    // убираем у логина все, после с символа @
                    if (login.Contains("@")) login = login.Substring(0, login.IndexOf('@'));

                    // получаем пользователя из БД
                    RKNet_Model.Account.User user = await db.Users
                        .Include(r => r.Roles)
                        .Include(g => g.Groups)
                        .Include(t => t.TTs)
                        .FirstOrDefaultAsync(u => u.Login.ToLower() == login);



                    // если пользователь есть в БД
                    if (user != null)
                    {                        
                        if (!user.Enabled)
                        {
                            ModelState.AddModelError("", "пользователь отключен");
                            return View(model);
                        }
                        
                        // пользователь AD
                        if (user.AdUser)
                        {
                            // проверка пароля через AD
                            var displayName = string.Empty;
                            var jobTitle = string.Empty;
                            var Error = string.Empty;

                            if (ADIdentity("shzhleb.ru", login, password, out displayName, out Error, out jobTitle))
                            {
                                // обновляем информацию о пользователе из Active Directory
                                user.Name = displayName;
                                user.JobTitle = jobTitle;

                                // обновляем список точек пользователя
                                if (user.AllTT)
                                {
                                    var newTTs = db.TTs.Where(t => !user.TTs.Contains(t)).Where(t => !t.Closed).ToList();
                                    user.TTs.AddRange(newTTs);
                                }


                                db.Users.Update(user);
                                db.SaveChanges();

                                // аутентификация
                                await Authenticate(user);
                                SaveSession(user);
                                return RedirectToAction("Index", "Home");
                            }
                            else
                            {
                                ModelState.AddModelError("", "неверное имя пользователя или пароль");
                                return View(model);
                            }
                        }
                        // локальный пользователь
                        else
                        {
                            // сравнение захешированного пароля
                            var verifyPassword = SecurePasswordHasher.Verify(model.password.ToLower(), user.Password);
                            if (verifyPassword)
                            //if (user.Password.ToLower() == password)
                            {
                                // обновляем список точек пользователя
                                if (user.AllTT)
                                {
                                    var newTTs = db.TTs.Where(t => !user.TTs.Contains(t)).Where(t => !t.Closed).ToList();
                                    user.TTs.AddRange(newTTs);
                                    db.Users.Update(user);
                                    db.SaveChanges();
                                }

                                await Authenticate(user);
                                SaveSession(user);
                                return RedirectToAction("Index", "Home");
                            }
                            else
                            {
                                ModelState.AddModelError("", "неверное имя пользователя или пароль");
                                return View(model);
                            }
                        }
                    }
                    // если пользователя нет в БД
                    else
                    {
                        // первый вход через AD
                        var displayName = string.Empty;
                        var jobTitle = string.Empty;
                        var Error = string.Empty;

                        if (ADIdentity("shzhleb.ru", login, password, out displayName, out Error, out jobTitle))
                        {
                            var adUser = new RKNet_Model.Account.User();
                            adUser.Name = displayName;
                            adUser.JobTitle = jobTitle;
                            adUser.Login = login;
                            adUser.AdUser = true;

                            db.Users.Add(adUser);
                            db.SaveChanges();

                            await Authenticate(adUser); // аутентификация
                            SaveSession(adUser);
                            return RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            ModelState.AddModelError("", "неверное имя пользователя или пароль");
                            return View(model);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError("", "неверное имя пользователя или пароль");
                }
                return View(model);
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }
        
        // Аутентификация
        private async Task Authenticate(RKNet_Model.Account.User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
                new Claim(ClaimTypes.WindowsAccountName, user.Login)
            };

            // получаем все собственные роли пользователя
            var roles = user.Roles.ToList();

            // добавляем роли групп, в которых состоит пользователь
            foreach (var group in user.Groups)
            {
                var rolesInGroup = db.Groups.Include(g => g.Roles).First(g => g.Id == group.Id).Roles;
                roles = roles.Union(rolesInGroup).ToList();
            }

            // присваеваем полученные роли сессии пользователя
            foreach(var role in roles)
            {
                claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, role.Code));
            }

            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));


            // фиксируем дату и время входа пользователя
            var lastLogin = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            var updateUser = db.Users.FirstOrDefault(u => u.Id == user.Id);
            updateUser.LastLogin = lastLogin;
            db.Users.Update(updateUser);
            db.SaveChanges();

            // логируем
            var log = new LogEvent<string>(user.Login);
            log.Name = "Аутентификация";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();
        }

        // Выход
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // Запрос данных авторизированного пользователя
        [Authorize]
        public IActionResult GetUserInfo()
        {
            var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            return new ObjectResult(user);
        }

        //---------------------------------------------------------------------------------------------

        // Аутентификация в ActiveDirectory
        private bool ADIdentity(string domain, string username, string password, out string fullName, out string Error, out string jobTitle)
        {
            fullName = string.Empty;
            jobTitle = string.Empty;
            Error = string.Empty;

            var domainAndUsername = string.Format("{0}\\{1}", domain, username);
            var ldapPath = "LDAP://dc1.shzhleb.ru";

            try
            {
                var entry = new DirectoryEntry(ldapPath, domainAndUsername, password);
                // Bind to the native AdsObject to force authentication.
                var obj = entry.NativeObject;
                var search = new DirectorySearcher(entry) { Filter = "(SAMAccountName=" + username + ")" };

                search.PropertiesToLoad.Add("displayName"); // Выводимое имя
                search.PropertiesToLoad.Add("title");

                try
                {
                    var result = search.FindOne();
                    fullName = result.GetDirectoryEntry().Properties["displayName"].Value.ToString();
                    jobTitle = result.GetDirectoryEntry().Properties["title"].Value.ToString();
                }
                catch (Exception e)
                {
                    Error = "ошибка получения свойств пользователя Active Directory: " + e.ToString();
                }
            }
            catch (Exception ex)
            {
                Error = "ошибка имени пользователя или пароля";
                return false;
            }

            return true;
        }


        public void SaveSession(RKNet_Model.Account.User user)
        {
            Models.MSSQL.UserSessions session = dbSql.UserSessions.FirstOrDefault(x => x.Id == user.Id);
            var oldSession = dbSql.UserSessions.FirstOrDefault(x => x.Id == user.Id)?.Date;

            if (session != null)
            {
                if(oldSession?.AddHours(1) < DateTime.Now)
                {
                    session.SessionID = HttpContext.Session.Id;
                    session.Date = DateTime.Now;
                    dbSql.SaveChanges();
                }
            }
            else
            {
                Portal.Models.MSSQL.UserSessions newSession = new Portal.Models.MSSQL.UserSessions();
                newSession.Id = user.Id;
                newSession.UserName = user.Name.ToLower();
                newSession.SessionID = HttpContext.Session.Id;
                newSession.Date = DateTime.Now;
                dbSql.UserSessions.Add(newSession);
                dbSql.SaveChanges();
            }
        }

        // Класс генерации и проверки пароля по хэшам
        public static class SecurePasswordHasher
        {
            private const int SaltSize = 16;
            private const int HashSize = 20;

            /// <summary>
            /// Creates a hash from a password.
            /// </summary>
            /// <param name="password">The password.</param>
            /// <param name="iterations">Number of iterations.</param>
            /// <returns>The hash.</returns>
            public static string Hash(string password, int iterations)
            {
                // Create salt
                byte[] salt;
                new RNGCryptoServiceProvider().GetBytes(salt = new byte[SaltSize]);

                // Create hash
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
                var hash = pbkdf2.GetBytes(HashSize);

                // Combine salt and hash
                var hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                // Convert to base64
                var base64Hash = Convert.ToBase64String(hashBytes);

                // Format hash with extra information
                return string.Format("$HASH$V1${0}${1}", iterations, base64Hash);
            }

            /// <summary>
            /// Creates a hash from a password with 10000 iterations
            /// </summary>
            /// <param name="password">The password.</param>
            /// <returns>The hash.</returns>
            public static string Hash(string password)
            {
                return Hash(password, 10000);
            }

            /// <summary>
            /// Checks if hash is supported.
            /// </summary>
            /// <param name="hashString">The hash.</param>
            /// <returns>Is supported?</returns>
            public static bool IsHashSupported(string hashString)
            {
                return hashString.Contains("$HASH$V1$");
            }

            /// <summary>
            /// Verifies a password against a hash.
            /// </summary>
            /// <param name="password">The password.</param>
            /// <param name="hashedPassword">The hash.</param>
            /// <returns>Could be verified?</returns>
            public static bool Verify(string password, string hashedPassword)
            {
                // Check hash
                if (!IsHashSupported(hashedPassword))
                {
                    throw new NotSupportedException("The hashtype is not supported");
                }

                // Extract iteration and Base64 string
                var splittedHashString = hashedPassword.Replace("$HASH$V1$", "").Split('$');
                var iterations = int.Parse(splittedHashString[0]);
                var base64Hash = splittedHashString[1];

                // Get hash bytes
                var hashBytes = Convert.FromBase64String(base64Hash);

                // Get salt
                var salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Create hash with given salt
                var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations);
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Get result
                for (var i = 0; i < HashSize; i++)
                {
                    if (hashBytes[i + SaltSize] != hash[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
