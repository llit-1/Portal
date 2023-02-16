using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Portal.Controllers
{
    [Authorize(Roles = "knowledge")]
    public class KnowledgeController : Controller
    {
        public IActionResult Welcome()
        {
            return PartialView();
        }

        public IActionResult Franch()
        {
            return PartialView();
        }
    }
}
