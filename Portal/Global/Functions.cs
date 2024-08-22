using Microsoft.AspNetCore.Http;
using Portal.Models.MSSQL;
using System;
using System.Linq;

namespace Portal.Global
{
    public static class Functions
    {
        public static bool CheckSessionID(DB.MSSQLDBContext dbSqlContext, string sessionID)
        {
            var session = dbSqlContext.UserSessions.FirstOrDefault(x => x.SessionID == sessionID);

            if (session == null)
            {
                return false;
            }

            if (DateTime.Now > session.Date.AddHours(1))
            {
                return false;
            }

            return true;
            
        }
    }
}
