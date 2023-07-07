using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;

namespace Portal.Controllers
{
    [Authorize]
    public class TimeTrackingController : Controller
    {
        public IActionResult TrackingData() 
        {
            return PartialView();
        }
    }
}
