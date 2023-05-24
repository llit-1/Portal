using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Portal.Models.PowerBi;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;
using Portal.Models;
using System.IO;
using Microsoft.EntityFrameworkCore;


namespace Portal.Controllers
{
    [Authorize(Roles ="reports")]
    public class ReportsController : Controller
    {
        DB.SQLiteDBContext db;

        public ReportsController(DB.SQLiteDBContext sqliteContext)
        {
            db = sqliteContext;
        }

        // Отчеты в общем доступе (без разграничения на уровне строк в отчетах)
        [Authorize(Roles = "reports_profit")]
        public ActionResult ProfitFree()
        {
            var reportsView = new ViewModels.Reports.ReportsView();

            var login = User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value.ToLower();
            reportsView.User = db.Users.Include(u => u.Reports).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
            reportsView.AllReports = db.AllReports.FirstOrDefault();            

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по выручке Free";
            log.Description = "/Reports/ProfitFree";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // План продаж
        [Authorize(Roles = "reports_profitplan")]
        public ActionResult ProfitPlan()
        {
            var reportsView = new ViewModels.Reports.ReportsView();

            var login = User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value.ToLower();
            reportsView.User = db.Users.Include(u => u.Reports).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт План продаж по ТТ";
            log.Description = "/Reports/ProfitPlan";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // Использование калькуляторов
        [Authorize(Roles = "reports_calcusage")]
        public IActionResult CalcUsage()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по калькуляторам";
            log.Description = "/Reports/CalcReport";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Время чеков
        [Authorize(Roles = "reports_checkstime")]
        public IActionResult ChecksTime()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "отчёт Время чеков";
            log.Description = "/Reports/CalcReport";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Чат GPT
        //[Authorize(Roles = "reports_calcusage")]
        public IActionResult GPT()
        {
            return PartialView();
        }

        // Датчики температуры
        //[Authorize(Roles = "reports_calcusage")]
        public IActionResult TemperatureSensors()
        {
            return PartialView();
        }

        // Кассовые операции
        [Authorize(Roles = "reports_cashoperations")]
        public IActionResult CashOperations()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "отчёт Кассовые опреации";
            log.Description = "/Reports/CashOperations";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public ActionResult ItsmFree()
        {
            var reportsView = new ViewModels.Reports.ReportsView();
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт ITSM Free";
            log.Description = "/Reports/ItsmFree";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // Про версии отчетов, требующие авторизации Microsoft Azure
        [Authorize(Roles = "reports_pro")]
        public ActionResult ProfitPro()
        {
            var reportsView = new ViewModels.Reports.ReportsView();

            var login = User.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value.ToLower();
            reportsView.User = db.Users.Include(u => u.Reports).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
            reportsView.AllReports = db.AllReports.FirstOrDefault();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по выручке Pro";
            log.Description = "/Reports/ProfitPro";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView(reportsView);
        }

        // Спец. версия отчета со служебными валютами
        [Authorize(Roles = "reports_special")]
        public ActionResult ProfitAll()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Отчёт по выручке All";
            log.Description = "/Reports/ProfitAll";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        // Другие отчёты
        [Authorize(Roles = "reports_other")]
        public ActionResult Other()
        {
            var rootPath = db.RootFolders.FirstOrDefault(r => r.Id == 13).Path;

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Другие отчёты";
            log.Description = "/Reports/Other";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return RedirectToAction("Folder", new { path = rootPath });
        }

        // Папка
        public IActionResult Folder(string path)
        {
            if(path == "Reports")
            {
                return Redirect("/Home/Reports");
            }

            var folderView = new ViewModels.Library.FolderView();
            
            // разэкранирование "плюс" и "пробел"
            path = path.Replace("plustoreplace", "+");
            path = path.Replace("backspacetoreplace", " ");

            // текущий каталог
            folderView.curDirectory = new DirectoryInfo(path);

            // корневой каталог раздела
            var rootItem = db.RootFolders.FirstOrDefault(r => path.Contains(r.Path));
            folderView.navItems.Add(rootItem);                        
            var rootDir = new DirectoryInfo(rootItem.Path);

            // промежуточные каталоги
            var curPath = folderView.curDirectory.FullName.Replace(rootDir.FullName, "");
            string[] dirs = curPath.Split('\\');

            var tempPath = rootDir.FullName;
            foreach (var dir in dirs)
            {
                if (dir.Length > 0)
                {
                    tempPath += "\\" + dir;
                    var item = new RKNet_Model.Library.RootFolder();
                    item.Name = dir;
                    item.Path = tempPath;
                    folderView.navItems.Add(item);
                }
            }

            // назад
            var itemsCount = folderView.navItems.Count;
            if (itemsCount > 1)
            {
                folderView.prevPath = folderView.navItems[itemsCount - 2].Path;
            }
            else
            {
                folderView.prevPath = "Reports";
            }
            
            return PartialView("Other", folderView);
        }

        // Встраиваемые отчёты Embedded
        public async Task<IActionResult> ProfitEmbedded()
        {
            return RedirectToAction("Unavailable","Home");

            // Параметры подключения к зарегистрированному приложению Power Bi
            PBIAppIntegration pbiApp = new PBIAppIntegration
            {
                ApiUrl = "https://api.powerbi.com/",
                ApplicationId = new Guid("947db2de-3e35-45ae-8af0-c2313c3218dd"),
                ApplicationSecret = "2xOYw_Tj9~S6u.a2W6J~JzK6D.~5ojKe.7",
                AuthorityUrl = "https://login.microsoftonline.com/b21b8975-765a-4273-8352-5c758cebf791/oauth2/token",
                EmbedUrlBase = "https://app.powerbi.com/",
                ResourceUrl = "https://analysis.windows.net/powerbi/api",
                UserName = "powerbi@shzhleb.ru",
                Password = "'ythubz-'ythubz=59"
            };

            // Парметры отчета Power Bi
            Models.PowerBi.Report report = new Models.PowerBi.Report
            {
                WorkspaceId = new Guid("20ad784f-9d5a-4988-9660-d1b9c3054841"),
                DataSet = "b63c39b9-b629-4484-921c-5ffc82ea20ef",
                ReportId = new Guid("cccaaf7e-0654-4afa-9e3f-4dd0bd982f17")           
            };

            // роли Power Bi
            List<string> pbiRoles = new List<string>();

            // присваиваем все роли пользователя ролям Power Bi
            foreach (var claim in User.Claims)
            {
                if(claim.Type == System.Security.Claims.ClaimsIdentity.DefaultRoleClaimType) pbiRoles.Add(claim.Value);
            }

            // выбираем версию отчета в зависимости от роли
            var firstRole = pbiRoles.FirstOrDefault();

            switch (firstRole)
            {
                case "typeRoleHere":
                    report.ReportId = new Guid("6dc259e6-80b3-4470-bee8-ca2c5e5c8bf7");
                    break;
                default:
                    break;
            }

            var reportConfig = await GetPBIEmbedConfig(pbiApp, report, pbiRoles);

            return PartialView(reportConfig);
        }        

        public async Task<IActionResult> ItsmEmbedded ()
        {
            return RedirectToAction("Unavailable", "Home");

            // Параметры подключения к зарегистрированному приложению Power Bi
            PBIAppIntegration pbiApp = new PBIAppIntegration
            {
                ApiUrl = "https://api.powerbi.com/",
                ApplicationId = new Guid("947db2de-3e35-45ae-8af0-c2313c3218dd"),
                ApplicationSecret = "2xOYw_Tj9~S6u.a2W6J~JzK6D.~5ojKe.7",
                AuthorityUrl = "https://login.microsoftonline.com/b21b8975-765a-4273-8352-5c758cebf791/oauth2/token",
                EmbedUrlBase = "https://app.powerbi.com/",
                ResourceUrl = "https://analysis.windows.net/powerbi/api",
                UserName = "powerbi@shzhleb.ru",
                Password = "'ythubz-'ythubz=59"
            };

            // Парметры отчета Power Bi
            Models.PowerBi.Report report = new Models.PowerBi.Report
            {
                WorkspaceId = new Guid("20ad784f-9d5a-4988-9660-d1b9c3054841"),
                ReportId = new Guid("31bbcc07-4ea5-420d-811e-2c07ec4b3717"),
                DataSet = "cc9f6a94-e221-42a4-a610-94771b8c3705"
            };

            // роли Power Bi
            List<string> pbiRoles = new List<string>();

            // присваиваем все роли пользователя ролям Power Bi
            //foreach (var claim in User.Claims)
            //{
            //if (claim.Type == System.Security.Claims.ClaimsIdentity.DefaultRoleClaimType) pbiRoles.Add(claim.Value);
            //}

            // Присваеваем роль all всем пользователям
            pbiRoles.Add("all");

            var reportConfig = await GetPBIEmbedConfig(pbiApp, report, pbiRoles);

            return PartialView(reportConfig);
        }

        // Отчёты в разработке
        public async Task<IActionResult> WorkReports()
        {
            // Параметры подключения к зарегистрированному приложению Power Bi
            PBIAppIntegration pbiApp = new PBIAppIntegration
            {
                ApiUrl = "https://api.powerbi.com/",
                ApplicationId = new Guid("947db2de-3e35-45ae-8af0-c2313c3218dd"),
                ApplicationSecret = "2xOYw_Tj9~S6u.a2W6J~JzK6D.~5ojKe.7",
                AuthorityUrl = "https://login.microsoftonline.com/b21b8975-765a-4273-8352-5c758cebf791/oauth2/token",
                EmbedUrlBase = "https://app.powerbi.com/",
                ResourceUrl = "https://analysis.windows.net/powerbi/api",
                UserName = "powerbi@shzhleb.ru",
                Password = "'ythubz-'ythubz=59"
            };

            // Парметры отчета Power Bi
            Models.PowerBi.Report report = new Models.PowerBi.Report
            {
                WorkspaceId = new Guid("1356f721-8012-421e-8dd6-5fff62fbdc89"),
                DataSet = "353e6db1-e56e-4ced-9da6-59afc19fe39b",
                ReportId = new Guid("bb149fcf-a8fb-4fce-8e92-d5ef93c7219d")
            };

            // роли Power Bi
            List<string> pbiRoles = new List<string>();

            // присваиваем все роли пользователя ролям Power Bi
            foreach (var claim in User.Claims)
            {
                if (claim.Type == System.Security.Claims.ClaimsIdentity.DefaultRoleClaimType) pbiRoles.Add(claim.Value);
            }

            var reportConfig = await GetPBIEmbedConfig(pbiApp, report, pbiRoles);

            return PartialView(reportConfig);
        }

        //служебные методы-----------------------------------------------------------------------------------------------------------

        // Запрос отчёта PowerBi
        private async Task<PowerBIEmbedConfig> GetPBIEmbedConfig(PBIAppIntegration pbiApp, Models.PowerBi.Report report, List<string> pbiRoles)
        {
            var result = new PowerBIEmbedConfig { Username = pbiApp.UserName };
            var accessToken = await GetPowerBIAccessToken(pbiApp);
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");

            using (var client = new PowerBIClient(new Uri(pbiApp.ApiUrl), tokenCredentials))
            {
                var workspaceId = report.WorkspaceId;
                var reportId = report.ReportId;
                var reportData = await client.Reports.GetReportInGroupAsync(workspaceId, reportId);

                // токен без ролей Power Bi
                //var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                //var tokenResponse = await client.Reports.GenerateTokenAsync(workspaceId, reportId, generateTokenRequestParameters);


                // токен с ролями Power Bi
                var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "View", null, identities: new List<EffectiveIdentity> { new EffectiveIdentity(username: pbiApp.UserName, roles: pbiRoles, datasets: new List<string> { report.DataSet }) });
                var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(workspaceId, reportId, generateTokenRequestParameters);

                result.EmbedToken = tokenResponse;
                result.EmbedUrl = reportData.EmbedUrl;
                result.Id = reportData.Id.ToString();                
            }

            return result;
        }

        // Получение токена PowerBi
        private async Task<string> GetPowerBIAccessToken(PBIAppIntegration pbiApp)
        {
            using (var client = new HttpClient())
            {
                var form = new Dictionary<string, string>();
                form["grant_type"] = "password";
                form["resource"] = pbiApp.ResourceUrl;
                form["username"] = pbiApp.UserName;
                form["password"] = pbiApp.Password;
                form["client_id"] = pbiApp.ApplicationId.ToString();
                form["client_secret"] = pbiApp.ApplicationSecret;
                form["scope"] = "openid";

                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                using (var formContent = new FormUrlEncodedContent(form))
                using (var response = await client.PostAsync(pbiApp.AuthorityUrl, formContent))
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var jsonBody = JObject.Parse(body);

                    var errorToken = jsonBody.SelectToken("error");
                    if (errorToken != null)
                    {
                        throw new Exception(errorToken.Value<string>());
                    }

                    return jsonBody.SelectToken("access_token").Value<string>();
                }
            }
        }

    }
}
