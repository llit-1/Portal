using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.ViewModels.SkuStop
{
    public class IndexView
    {
        // данные
        public List<RKNet_Model.MSSQL.SkuStop> stopList;
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

            stopList = new List<RKNet_Model.MSSQL.SkuStop>();
            createdDates = new List<string>();
            userNames = new List<string>();
            states = new List<string>();
        }
    }
}
