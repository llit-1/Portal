using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Portal.Controllers
{
    [Authorize(Roles = "calculators")]
    public class CalculatorController : Controller
    {
        public IActionResult Vipechka()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора выпечки";
            log.Description = "/Calculator/Vipechka";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public IActionResult Conditerka()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора кондитерки";
            log.Description = "/Calculator/Conditerka";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }

        public IActionResult Sandwitches()
        {
            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "Запуск калькулятора сэндвичей";
            log.Description = "/Calculator/Sandwitches";
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return PartialView();
        }
        
    }
}
