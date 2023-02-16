using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.UserRequests
{
    public class RequestsListView
    {
        // данные
        public List<Models.MSSQL.UserRequest> requests;        

        // количество логов на странице
        public int countRows;
        public int rowsOnPage;
        public int selectedPage;

        // фильтры
        public string stateFilter;

        public RequestsListView()
        {
            rowsOnPage = 15;
            selectedPage = 1;

            requests = new List<Models.MSSQL.UserRequest>();
            stateFilter = "";
        }
    }
}
