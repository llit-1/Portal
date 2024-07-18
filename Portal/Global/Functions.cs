using System;
using System.Linq;

namespace Portal.Global
{
    public static class Functions
    {
        public static bool CheckSessionID(DB.MSSQLDBContext dbSqlContext, string sessionID)
        {
            if (dbSqlContext.UserSessions.FirstOrDefault(x => x.SessionID == sessionID) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
