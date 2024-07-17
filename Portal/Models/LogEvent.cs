using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RKNet_Model;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Portal.Models
{
    public class LogEvent<T>
    {        
        public string Name;
        public string Description;
        public string IpAdress;
        public T Object;
        private RKNet_Model.Account.User User;
        private DB.SQLiteDBContext sqlite;
        private DB.MSSQLDBContext mssql;
        private bool enabled;        

        // конструктор без параметров
        public LogEvent()
        {
            // базы данных
            var sqliteBuilder = new DbContextOptionsBuilder<DB.SQLiteDBContext>();
            sqliteBuilder.UseSqlite(SettingsInternal.Configuration.GetConnectionString("sqlite"));
            sqlite = new DB.SQLiteDBContext(sqliteBuilder.Options);

            var mssqlBuilder = new DbContextOptionsBuilder<DB.MSSQLDBContext>();
            mssqlBuilder.UseSqlServer(SettingsInternal.Configuration.GetConnectionString("mssql"));
            mssql = new DB.MSSQLDBContext(mssqlBuilder.Options);

            // данные пользователя
            User = new RKNet_Model.Account.User();

            // выключатель модуля
            enabled = sqlite.Modules.FirstOrDefault(m => m.Name == "Logging").Enabled;
        }

        // конструктор по клэймам пользователя
        public LogEvent(System.Security.Claims.ClaimsPrincipal curUser)
        {
            // базы данных
            var sqliteBuilder = new DbContextOptionsBuilder<DB.SQLiteDBContext>();
            sqliteBuilder.UseSqlite(SettingsInternal.Configuration.GetConnectionString("sqlite"));
            sqlite = new DB.SQLiteDBContext(sqliteBuilder.Options);

            var mssqlBuilder = new DbContextOptionsBuilder<DB.MSSQLDBContext>();
            mssqlBuilder.UseSqlServer(SettingsInternal.Configuration.GetConnectionString("mssql"));
            mssql = new DB.MSSQLDBContext(mssqlBuilder.Options);

            // данные пользователя
            var login = curUser.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            User = sqlite.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            // выключатель модуля
            enabled = sqlite.Modules.FirstOrDefault(m => m.Name == "Logging").Enabled;
        }

        // конструктор по логину
        public LogEvent(string login)
        {
            // базы данных
            var sqliteBuilder = new DbContextOptionsBuilder<DB.SQLiteDBContext>();
            sqliteBuilder.UseSqlite(SettingsInternal.Configuration.GetConnectionString("sqlite"));
            sqlite = new DB.SQLiteDBContext(sqliteBuilder.Options);

            var mssqlBuilder = new DbContextOptionsBuilder<DB.MSSQLDBContext>();
            mssqlBuilder.UseSqlServer(SettingsInternal.Configuration.GetConnectionString("mssql"));
            mssql = new DB.MSSQLDBContext(mssqlBuilder.Options);

            // данные пользователя            
            User = sqlite.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            // выключатель модуля
            enabled = sqlite.Modules.FirstOrDefault(m => m.Name == "Logging").Enabled;
        }
        private IHttpClientFactory _httpClientFactory;

        // запись лога в БД
        public void Save(string userAgent = "", string SessionID = "")
        {
            if (enabled)
            {
                var log = new RKNet_Model.MSSQL.Log();
                log.dateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                log.userId = User.Id;
                log.userName = User.Name;
                log.userJobTitle = User.JobTitle;
                // Определяем браузер
                if (userAgent.Contains("MSIE"))
                {
                    log.Browser = "Internet Explorer";
                }
                else if (userAgent.Contains("Chrome"))
                {
                    log.Browser = "Chrome";
                }
                else if (userAgent.Contains("Firefox"))
                {
                    log.Browser = "Firefox";
                }
                else if (userAgent.Contains("Safari") && userAgent.Contains("Version"))
                {
                    log.Browser = "Safari";
                }
                else
                {
                    log.Browser = "Unknown";
                }

                // Определяем операционную систему
                if (userAgent.Contains("Windows NT 10.0"))
                {
                    log.OS = "Windows 10";
                }
                else if (userAgent.Contains("Windows NT 6.3"))
                {
                    log.OS = "Windows 8.1";
                }
                else if (userAgent.Contains("Windows NT 6.2"))
                {
                    log.OS = "Windows 8";
                }
                else if (userAgent.Contains("Windows NT 6.1"))
                {
                    log.OS = "Windows 7";
                }
                else if (userAgent.Contains("Windows NT 6.0"))
                {
                    log.OS = "Windows Vista";
                }
                else if (userAgent.Contains("Windows NT 5.1") || userAgent.Contains("Windows NT 5.2"))
                {
                    log.OS = "Windows XP";
                }
                else if (userAgent.Contains("Mac OS X"))
                {
                    int startIndex = userAgent.IndexOf("Mac OS X") + "Mac OS X".Length + 1;
                    int endIndex = userAgent.IndexOf(")", startIndex);
                    if (startIndex < endIndex)
                    {
                        log.OS = userAgent.Substring(startIndex, endIndex - startIndex);
                    }
                    else
                    {
                        log.OS = "Mac OS X";
                    }
                }
                else if (userAgent.Contains("Android"))
                {
                    // Для Android можно выделить версию операционной системы
                    // Например:
                    int startIndex = userAgent.IndexOf("Android") + "Android".Length + 1;
                    int endIndex = userAgent.IndexOf(";", startIndex);
                    if (startIndex < endIndex)
                    {
                        log.OS = userAgent.Substring(startIndex, endIndex - startIndex);
                    }
                    else
                    {
                        log.OS = "Android";
                    }
                }
                else
                {
                    log.OS = "Unknown";
                }
                log.SessionID = SessionID;
                log.Name = Name;
                log.IpAdress = IpAdress;
                log.Description = Description;
                log.Json = JsonConvert.SerializeObject(Object);
                log.PortalVersion = SettingsInternal.portalVersion;

                mssql.Logs.Add(log);
                mssql.SaveChanges();
            }
        }

        // проверка обновления версии
        public bool isNewVersion()
        {
            
            var result = false;
            var lastLog = mssql.Logs.OrderBy(i => i.Id).LastOrDefault();
            if (lastLog != null)
            {
                if (lastLog.PortalVersion != SettingsInternal.portalVersion)
                {
                    result = true;
                }
            }                

            return result;
        }
    }
}
