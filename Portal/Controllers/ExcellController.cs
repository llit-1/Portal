using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class ExcellController : Controller
    {
        public IActionResult Demo()
        {
            return PartialView();
        }
    }
}
