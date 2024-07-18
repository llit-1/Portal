using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;

namespace Portal.Models.MSSQL
{
    public class UserSessions
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string SessionID { get; set; }
        public DateTime Date { get; set; }
    }
}
