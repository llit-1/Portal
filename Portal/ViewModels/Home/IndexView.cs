using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Home
{
    public class IndexView
    {
        public System.Security.Claims.ClaimsPrincipal User;
        public List<string> roleRequests;
    }
}
