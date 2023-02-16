using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RKNet_Model;

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

        // запись лога в БД
        public void Save()
        {                                    
            if (enabled)
            {                           
                var log = new RKNet_Model.MSSQL.Log();

                log.dateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                log.userId = User.Id;
                log.userName = User.Name;
                log.userJobTitle = User.JobTitle;
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
