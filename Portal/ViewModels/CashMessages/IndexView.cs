using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.CashMessages
{
    public class IndexView
    {
        // данные
        public List<Models.MSSQL.CashMessage> cashMessages;
        public List<string> createdDates;
        public List<string> userNames;
        public List<string> states;        

        // параметры строк
        public int countRows;
        public int rowsOnPage;
        public int selectedPage;

        // фильтры
        public string createDate;
        public string userName;
        public string state;
        public string finished;

        public IndexView()
        {
            rowsOnPage = 15;
            selectedPage = 1;

            createDate = "";
            userName = "";
            state = "";
            finished = "";

            cashMessages = new List<Models.MSSQL.CashMessage>();
            createdDates = new List<string>();
            userNames = new List<string>();
            states = new List<string>();
        }        
    }
}
