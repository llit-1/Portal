using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Portal.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;


using System.IO;

namespace Portal.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private DB.SQLiteDBContext db;
        private DB.MSSQLDBContext mssql;
       
        public HomeController(ILogger<HomeController> logger, DB.SQLiteDBContext context, DB.MSSQLDBContext mssqlContext)
        {
            _logger = logger;
            db = context;
            mssql = mssqlContext;            
        }

        // сохраняем ip пользователя в сессию
        public IActionResult SetIp(string userIp)
        {
            HttpContext.Session.SetString("ip", userIp);
            return new ObjectResult("Ok");
        }

        // Главная страница
        public IActionResult Index()
        {
            var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            // получаем запрошенные пользователем роли
            var userRequests = mssql.UserRequests.Where(r => r.State != "closed").Where(r => r.UserId == user.Id).ToList();

            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isAjax)
            {
                return PartialView(userRequests);
            }
            else
            {
                return View(userRequests);
            }
        }

        // Отчёты
        [Authorize(Roles = "reports")]
        public IActionResult Reports()
        {            
            return PartialView();
        }

        // Заказы ТТ
        [Authorize(Roles = "ttorders")]
        public IActionResult TTOrders()
        {
            return PartialView();
        }

        // Заявки 1С
        [Authorize(Roles = "sd1c")]
        public IActionResult ServiceDesk_1C()
        {
            return PartialView();
        }

        // Калькуляторы
        [Authorize(Roles = "calculators")]
        public IActionResult Calculators()
        {
            return PartialView();
        }

        // Учет сотрудников
        [Authorize(Roles = "employee_control, employee_control_factory")]
        public IActionResult Staff()
        {
            return PartialView();
        }


        // Библиотека знаний (старая)
        [Authorize(Roles = "knowledge")]
        public IActionResult Knowledge()
        {
            var result = new RKNet_Model.Result<DirectoryInfo[]>();

            result.Data = new DirectoryInfo[2];


            try
            {                
                var path1 = @"\\shzhleb.ru\ll\LLWork\Библиотека знаний";
                var path2 = @"\\shzhleb.ru\shz\SHZWork\Франшиза";
                result.Data[0] = new DirectoryInfo(path1);
                result.Data[1] = new DirectoryInfo(path2);
            }

            catch (Exception e)
            {
                result.Ok = false;
                result.ErrorMessage = e.Message;
                result.ExceptionText = e.ToString();
            }
            return PartialView(result);
        }
        
        [AllowAnonymous]
        public IActionResult KnowledgeGetFile(string filepath)
        {            
            try
            {
                filepath = filepath.Replace("plustoreplace", "+");
                var fileInfo = new System.IO.FileInfo(filepath);
                var file = System.IO.File.ReadAllBytes(filepath);
                
                switch (fileInfo.Extension.ToLower())
                {
                    case ".doc":
                    case ".docx":
                        return new FileContentResult(file, "application/msword");
                    case ".xls":
                    case ".xlsx":
                        return new FileContentResult(file, "application/vnd.ms-excel");
                    case ".ppt":
                    case ".pptx":
                        return new FileContentResult(file, "application/vnd.ms-powerpoint");
                    case ".pdf":
                        return new FileContentResult(file, "application/pdf");
                    case ".jpg":
                    case ".jpeg":
                        return new FileContentResult(file, "image/jpeg");
                    case ".gif":
                        return new FileContentResult(file, "image/gif");
                    case ".png":
                        return new FileContentResult(file, "image/png");
                    case ".avi":
                        return new FileContentResult(file, "video/avi");
                    case ".mov":
                        return new FileContentResult(file, "video/quicktime");
                    case ".mp4":
                        return new FileContentResult(file, "video/mp4");
                    case ".mpeg":
                        return new FileContentResult(file, "video/mpeg");
                    default:
                        return new EmptyResult();

                }
            }
            catch(Exception e)
            {
                return new ObjectResult(e.Message);
            }

            
        }

        // Прогноз продаж
        [Authorize(Roles = "salesprediction")]
        public IActionResult SalesPrediction()
        {     
            try
            {                
                return PartialView();
            }

            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }

           
        }

        // 1C
        [Authorize(Roles = "1c")]
        public IActionResult C1()
        {
            return PartialView();
        }

        // Видеонаблюдение
        [Authorize(Roles = "video")]
        public IActionResult Video()
        {
            return PartialView();
        }

        // Yammer
        public IActionResult Yammer()
        {
            return PartialView();
        }



        // Настройки
        [Authorize(Roles = "settings,TTSettings,HR")]
        public IActionResult Settings()
        {
            return PartialView();
        }

        // Сервис временно недоступен
        public IActionResult Unavailable()
        {
            return PartialView();
        }
        
        // Разверните экран
        public IActionResult RotateToHorizontal()
        {
            return PartialView();
        }
        //----------------------------------------------------------------------------------------------------------------

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}