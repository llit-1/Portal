using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Models.MSSQL
{
    public class UserRequest
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Type { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserJobTitle { get; set; }
        public string Message { get; set; }
        public int RoleId { get; set; }
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public string State { get; set; }
    }
}
