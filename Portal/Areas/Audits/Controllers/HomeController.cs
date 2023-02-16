using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Areas.Audits.Controllers
{
    [Area("Audits")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return PartialView();
        }
    }
}
