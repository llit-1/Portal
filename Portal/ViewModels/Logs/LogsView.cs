using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.Logs
{
    public class LogsView
    {
        // данные
        public List<RKNet_Model.MSSQL.Log> Logs;
        public List<string> UserNames;
        public List<string> Actions;
        public List<string> Versions;
        public List<string> UserIps;

        // количество логов на странице
        public int countLogs;
        public int logsOnPage;
        public int selectedPage;

        // фильтры
        public string userName;
        public string actionName;
        public string version;
        public string userIp;
        public string date;
        
        

        public LogsView()
        {
            logsOnPage = 15;
            selectedPage = 1;

            UserNames = new List<string>();
            Actions = new List<string>();
            Versions = new List<string>();
            UserIps = new List<string>();

            userName = "";
            actionName = "";
            version = "";
            userIp = "";
            date = "";
        }
    }
}
