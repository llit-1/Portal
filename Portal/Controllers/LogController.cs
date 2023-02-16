using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Portal.Controllers
{
    [Authorize(Roles = "logs")]
    public class LogController : Controller
    {
        DB.MSSQLDBContext mssql;

        public LogController(DB.MSSQLDBContext mssqlContext)
        {
            mssql = mssqlContext;
        }

        public IActionResult Index(string userName, string actionName, string version, string selectedPage, string logsOnPage, string userIp, string date)
        {
            try
            {
                var logsView = new ViewModels.Logs.LogsView();

                // фильтр даты
                DateTime Date;
                var isDateOk = DateTime.TryParse(date, out Date);

                // фильтр имени пользователя
                if (userName != null)
                {
                    userName = userName.Replace("pp", "+");
                    userName = userName.Replace("bb", " ");
                    logsView.userName = userName;
                }
                // фильтр события
                if (actionName != null)
                {
                    actionName = actionName.Replace("pp", "+");
                    actionName = actionName.Replace("bb", " ");
                    logsView.actionName = actionName;
                }
                // фильтр версии
                if (version != null)
                {
                    version = version.Replace("pp", "+");
                    version = version.Replace("bb", " ");
                    logsView.version = version;
                }
                // фильтр ip адреса
                if (userIp != null)
                {
                    userIp = userIp.Replace("pp", "+");
                    userIp = userIp.Replace("bb", " ");
                    logsView.userIp = userIp;
                }
                // выбранная страница
                if (selectedPage != null)
                {
                    logsView.selectedPage = int.Parse(selectedPage);
                }
                // количество строк на странице
                if (logsOnPage != null)
                {
                    logsView.logsOnPage = int.Parse(logsOnPage);
                }

                // получаем данные для фильтров
                logsView.UserNames = mssql.Logs.Select(u => u.userName).Where(u => u.Length > 0).Distinct().ToList();
                logsView.Actions = mssql.Logs.Select(a => a.Name).Where(u => u.Length > 0).Distinct().ToList();
                logsView.UserIps = mssql.Logs.Select(i => i.IpAdress).Where(u => u.Length > 0).Distinct().ToList();

                var versions = mssql.Logs.Select(v => new { v.PortalVersion, v.dateTime }).OrderByDescending(d => d.dateTime).ToList();
                logsView.Versions = versions.Select(v => v.PortalVersion).Distinct().ToList();

                // задан фильтр даты
                if(isDateOk)
                {
                    logsView.date = Date.ToString("yyyy-MM-dd");
                    // получаем отфильтрованные данные из БД и разбиваем на страницы
                    logsView.countLogs = mssql.Logs
                        .Where(
                            l => l.userName.Contains(logsView.userName) &
                            l.Name.Contains(logsView.actionName) &
                            l.PortalVersion.Contains(logsView.version) &
                            l.IpAdress.Contains(logsView.userIp) &
                            l.dateTime.Date == Date)                            
                        .Count();

                    if (logsView.countLogs > logsView.logsOnPage)
                    {
                        logsView.Logs = mssql.Logs
                            .Where(
                                l => l.userName.Contains(logsView.userName) &
                                l.Name.Contains(logsView.actionName) &
                                l.PortalVersion.Contains(logsView.version) &
                                l.IpAdress.Contains(logsView.userIp) &
                                l.dateTime.Date == Date)
                            .OrderBy(date => date.dateTime)
                            .Skip(logsView.countLogs - logsView.logsOnPage * logsView.selectedPage)
                            .Take(logsView.logsOnPage)
                            .ToList();
                    }
                    else
                    {
                        logsView.Logs = mssql.Logs
                            .Where(l =>
                                l.userName.Contains(logsView.userName) &
                                l.Name.Contains(logsView.actionName) &
                                l.PortalVersion.Contains(logsView.version) &
                                l.IpAdress.Contains(logsView.userIp) &
                                l.dateTime.Date == Date)
                            .ToList();
                    }
                }
                // не задан фильтр даты
                else
                {
                    logsView.date = "";
                    // получаем отфильтрованные данные из БД и разбиваем на страницы
                    logsView.countLogs = mssql.Logs
                        .Where(
                            l => l.userName.Contains(logsView.userName) & 
                            l.Name.Contains(logsView.actionName) & 
                            l.PortalVersion.Contains(logsView.version) & 
                            l.IpAdress.Contains(logsView.userIp))
                        .Count();

                    if (logsView.countLogs > logsView.logsOnPage)
                    {
                        logsView.Logs = mssql.Logs
                            .Where(
                                l => l.userName.Contains(logsView.userName) & 
                                l.Name.Contains(logsView.actionName) & 
                                l.PortalVersion.Contains(logsView.version) & 
                                l.IpAdress.Contains(logsView.userIp))
                            .OrderBy(date => date.dateTime)
                            .Skip(logsView.countLogs - logsView.logsOnPage * logsView.selectedPage)
                            .Take(logsView.logsOnPage)
                            .ToList();
                    }
                    else
                    {
                        logsView.Logs = mssql.Logs
                            .Where(l => 
                                l.userName.Contains(logsView.userName) & 
                                l.Name.Contains(logsView.actionName) & 
                                l.PortalVersion.Contains(logsView.version) & 
                                l.IpAdress.Contains(logsView.userIp))
                            .ToList();
                    }
                }
                

                return PartialView(logsView);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }
    }
}
