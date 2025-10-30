using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using Portal.DB;
using Portal.Models;
using Portal.Models.MSSQL.Reports1C;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static RKNet_Model.Rk7XML.Response.GetSystemInfo2Response;


namespace Portal.Controllers
{
    [Authorize(Roles = "ttorders")]
    public class ttOrdersController : Controller
    {
        DB.MSSQLDBContext mssql;
        DB.SQLiteDBContext db;
        private DB.Reports1CDBContext reports1CSql;

        public ttOrdersController(DB.MSSQLDBContext contextMssql, DB.SQLiteDBContext contextSqlite, DB.Reports1CDBContext reports1CDBContext)
        {
            mssql = contextMssql;
            db = contextSqlite;
            reports1CSql = reports1CDBContext;
        }

        // Список заказов
        public IActionResult OrdersList(string dateString, string ttString, int orderTypeId)
        {
            try
            {
                var ordersView = new ViewModels.FranchOrdersView();
                ordersView.orderType = db.OrderTypes.FirstOrDefault(t => t.Id == orderTypeId);

                // фильтр по дате
                if (dateString == null) dateString = "current";

                ordersView.selectedDate = dateString;
                DateTime date;
                DateTime monthBegin;
                switch (dateString)
                {
                    case "current":
                        var today = DateTime.Now;
                        date = new DateTime(today.Year, today.Month, today.Day);
                        monthBegin = new DateTime(today.Year, today.Month, 1);
                        ordersView.forders = mssql.FranchOrders.Where(o => o.DeliveryDate >= date & o.OrderTypeId == orderTypeId).ToList();
                        ordersView.ThisMonthForders = mssql.FranchOrders.Where(o => o.DeliveryDate >= monthBegin & o.DeliveryDate < date & o.OrderTypeId == orderTypeId).ToList();
                        break;
                    case "all":
                        ordersView.forders = mssql.FranchOrders.Where(o => o.OrderTypeId == orderTypeId).ToList();
                        break;
                    default:
                        date = DateTime.ParseExact(dateString, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        monthBegin = new DateTime(date.Year, date.Month, 1);
                        ordersView.forders = mssql.FranchOrders.Where(o => o.DeliveryDate == date & o.OrderTypeId == orderTypeId).ToList();
                        ordersView.ThisMonthForders = mssql.FranchOrders.Where(o => o.DeliveryDate >= monthBegin & o.DeliveryDate < date & o.OrderTypeId == orderTypeId).ToList();
                        break;
                }

                // заказы по ТТ пользователя
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.Include(t => t.TTs).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                var userTTs = new List<int>();

                ordersView.TTs = user.TTs.OrderBy(t => t.Name).ToList();
                userTTs = user.TTs.Select(t => t.Code).ToList();
                ordersView.forders = ordersView.forders.Where(o => userTTs.Contains(o.TTCode)).ToList();

                // фильтр по одной выбранной ТТ
                if (ttString == null) ttString = "all";
                ordersView.selectedTT = ttString;

                if (ttString != "all")
                {
                    ordersView.forders = ordersView.forders.Where(o => o.TTCode == int.Parse(ttString)).ToList();
                }

                // список дат
                ordersView.deliveryDates = mssql.FranchOrders.Where(o => o.OrderTypeId == orderTypeId & userTTs.Contains(o.TTCode)).Select(o => o.DeliveryDate).Distinct().OrderBy(n => n.Date).ToList();

                return PartialView(ordersView);
            }

            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Редактор заказа
        public IActionResult OrderEdit(string mode, int orderNumber, int orderTypeId)
        {
            var fordresView = new ViewModels.FranchOrdersView();
            fordresView.orderEditNumber = orderNumber;
            fordresView.orderType = db.OrderTypes.FirstOrDefault(t => t.Id == orderTypeId);

            try
            {
                // получаем группы SKU из эксель файла (справочника SKU)
                FileInfo excelFile = null;
                string listName = null;

                switch (orderTypeId)
                {
                    case 1:
                        excelFile = new FileInfo(SettingsInternal.TTOrders.franchPath);
                        listName = SettingsInternal.TTOrders.franchList;
                        break;
                    case 2:
                        excelFile = new FileInfo(SettingsInternal.TTOrders.weekPath);
                        listName = SettingsInternal.TTOrders.weekList;
                        break;
                    case 3:
                        excelFile = new FileInfo(SettingsInternal.TTOrders.monthPath);
                        listName = SettingsInternal.TTOrders.monthList;
                        break;
                    case 4:
                        excelFile = new FileInfo(SettingsInternal.TTOrders.pricesPath);
                        listName = SettingsInternal.TTOrders.pricesList;
                        break;
                    default:
                        break;
                }

                using (ExcelPackage excelPackage = new ExcelPackage(excelFile))
                {
                    ExcelWorksheet production = excelPackage.Workbook.Worksheets.FirstOrDefault(x => x.Name == listName);
                    var rowsCount = production.Dimension.End.Row;

                    for (var x = 2; x <= rowsCount; x++)
                    {
                        var group = new ViewModels.FranchOrdersView.Group();
                        switch (orderTypeId)
                        {
                            case 1:
                            case 2:
                            case 3:
                                group.Name = production.Cells[x, 4].Value.ToString();
                                group.DeliveryDate = production.Cells[x, 7].Value.ToString();
                                break;
                            case 4:
                                group.Name = production.Cells[x, 2].Value.ToString();
                                group.DeliveryDate = "В последний день месяца";
                                break;
                        }


                        fordresView.Groups.Add(group);
                    }
                }

                // получаем данные пользователя
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.Include(u => u.TTs).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                // фильтруем список точек
                fordresView.TTs = user.TTs.Where(t => !t.Closed).OrderBy(t => t.Name).ToList();

                List<Models.MSSQL.FranchOrder> items = new List<Models.MSSQL.FranchOrder>();
                fordresView.mode = mode;

                LogEvent<int> log;
                var orderType = db.OrderTypes.FirstOrDefault(t => t.Id == orderTypeId);

                switch (mode)
                {
                    case "new":
                        fordresView.orderNumber = int.Parse(DateTime.Now.ToString("MMddHHmmss")); // генерируем номер заказа                        

                        // логируем
                        log = new LogEvent<int>(User);
                        log.Name = "Создание заказа";
                        log.Description = orderType.Name;
                        log.Object = fordresView.orderNumber;
                        log.IpAdress = HttpContext.Session.GetString("ip");
                        log.Save();

                        break;

                    case "edit":
                        items = mssql.FranchOrders.Where(f => f.OrderNumber == orderNumber).ToList();
                        fordresView.forders = items;

                        // логируем
                        log = new LogEvent<int>(User);
                        log.Name = "Изменение заказа";
                        log.Description = orderType.Name;
                        log.Object = orderNumber;
                        log.IpAdress = HttpContext.Session.GetString("ip");
                        log.Save();

                        break;

                    case "copy":
                        items = mssql.FranchOrders.Where(f => f.OrderNumber == orderNumber).ToList();
                        fordresView.forders = items;

                        fordresView.orderEditNumber = int.Parse(DateTime.Now.ToString("MMddHHmmss")); // генерируем номер заказа

                        // логируем
                        log = new LogEvent<int>(User);
                        log.Name = "Копирование заказа";
                        log.Description = orderType.Name;
                        log.Object = fordresView.orderEditNumber;
                        log.IpAdress = HttpContext.Session.GetString("ip");
                        log.Save();

                        var OrdersItems = new List<FOrderXLS>();

                        if(orderTypeId == 1)
                        {
                            using (ExcelPackage excelPackage = new ExcelPackage(new FileInfo(SettingsInternal.TTOrders.franchPath)))
                            {
                                ExcelWorksheet production = excelPackage.Workbook.Worksheets.FirstOrDefault(x => x.Name == SettingsInternal.TTOrders.franchList);
                                var rowsCount = production.Dimension.End.Row;

                                for (var x = 2; x <= rowsCount; x++)
                                {
                                    var forder = new FOrderXLS();

                                    // фильтр по признаку собственное управление / партнерское управление
                                    forder.Article = production.Cells[x, 1].Value.ToString();
                                    var enabled = production.Cells[x, 8].Value.ToString();

                                    // применение фильтров
                                    if (enabled == "1")
                                    {
                                        OrdersItems.Add(forder);
                                    }

                                }
                            }
                            var onlyArticles = OrdersItems.Select(x => x.Article).ToList();
                            fordresView.forders = items.Where(x => onlyArticles.Contains(x.Article)).ToList();
                        }
                        
                        break;

                    default:
                        break;
                }


                var OrdersTable = new List<FOrderXLS>();
                using (ExcelPackage excelPackage = new ExcelPackage(excelFile))
                {
                    ExcelWorksheet production = excelPackage.Workbook.Worksheets.FirstOrDefault(x => x.Name == listName);
                    var rowsCount = production.Dimension.End.Row;

                    for (var x = 2; x <= rowsCount; x++)
                    {
                        var forder = new FOrderXLS();

                        // фильтр по признаку собственное управление / партнерское управление
                        var typeFilter = "0";
                        var enabled = "0";
                        // фильтр отключенных позиций для всех
                        switch (orderTypeId)
                        {

                            case 4:
                                forder.Sku = production.Cells[x, 1].Value.ToString();
                                var skuString = forder.Sku.Replace(" ", "").Replace("\"", "").Replace("(", "").Replace(")", "").Replace(",", "").Replace(".", "");
                                var maxLength = 15;
                                if (skuString.Length > maxLength)
                                    skuString = skuString.Substring(0, maxLength);

                                forder.Article = production.Cells[x, 5].Value.ToString();
                                forder.MinOrder = "1";
                                forder.Group = production.Cells[x, 2].Value.ToString();
                                forder.FormingDate = "Последний день месяца";
                                forder.FormingTime = "23:00";
                                forder.DeliveryDate = "В последний день месяца";
                                forder.MaxOrder = production.Cells[x, 4].Value.ToString();
                                enabled = production.Cells[x, 3].Value.ToString();
                                typeFilter = "1";
                                break;
                        }
                        var skuFilter = "1";
                        if (enabled == "1" & typeFilter == "1" & skuFilter == "1")
                            OrdersTable.Add(forder);
                    }
                }
                fordresView.items = OrdersTable;
                return PartialView(fordresView);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Список позиций для заказа
        public IActionResult SkuList(string group, string delivery, int orderTypeId)
        {
            var ordersView = new ViewModels.FranchOrdersView();
            try
            {
                ordersView.orderType = db.OrderTypes.FirstOrDefault(o => o.Id == orderTypeId);

                // возвращаем экранированные символы
                group = group.Replace("backspacetoreplace", " ");
                group = group.Replace("plustoreplace", "+");

                delivery = delivery.Replace("backspacetoreplace", " ");
                delivery = delivery.Replace("plustoreplace", "+");

                // читаем позиции в эксель файле
                FileInfo excellFile = null;
                string listName = null;

                switch (orderTypeId)
                {
                    case 1:
                        excellFile = new FileInfo(SettingsInternal.TTOrders.franchPath);
                        listName = SettingsInternal.TTOrders.franchList;
                        break;
                    case 2:
                        excellFile = new FileInfo(SettingsInternal.TTOrders.weekPath);
                        listName = SettingsInternal.TTOrders.weekList;
                        break;
                    case 3:
                        excellFile = new FileInfo(SettingsInternal.TTOrders.monthPath);
                        listName = SettingsInternal.TTOrders.monthList;
                        break;
                    case 4:
                        excellFile = new FileInfo(SettingsInternal.TTOrders.pricesPath);
                        listName = SettingsInternal.TTOrders.pricesList;
                        break;
                    default:
                        break;
                }

                var OrdersTable = new List<FOrderXLS>();

                using (ExcelPackage excelPackage = new ExcelPackage(excellFile))
                {
                    ExcelWorksheet production = excelPackage.Workbook.Worksheets.FirstOrDefault(x => x.Name == listName);
                    var rowsCount = production.Dimension.End.Row;

                    for (var x = 2; x <= rowsCount; x++)
                    {
                        var forder = new FOrderXLS();

                        // фильтр по признаку собственное управление / партнерское управление
                        var typeFilter = "0";
                        var enabled = "0";
                        // фильтр отключенных позиций для всех


                        switch (orderTypeId)
                        {
                            case 1:
                            case 2:
                            case 3:
                                if (production.Cells[x, 1].Value != null)
                                    forder.Article = production.Cells[x, 1].Value.ToString().Replace(" ", "").Trim();
                                forder.Sku = production.Cells[x, 2].Value.ToString();
                                forder.MinOrder = production.Cells[x, 3].Value.ToString();
                                forder.Group = production.Cells[x, 4].Value.ToString();
                                forder.FormingDate = production.Cells[x, 5].Value.ToString();
                                forder.FormingTime = production.Cells[x, 6].Value.ToString();
                                forder.DeliveryDate = production.Cells[x, 7].Value.ToString();

                                enabled = production.Cells[x, 8].Value.ToString();

                                if (User.IsInRole("ttorders_ll"))
                                {
                                    typeFilter = production.Cells[x, 9].Value.ToString();
                                }

                                if (User.IsInRole("ttorders_franch"))
                                {
                                    typeFilter = production.Cells[x, 10].Value.ToString();
                                }

                                break;
                            case 4:
                                forder.Sku = production.Cells[x, 1].Value.ToString();
                                var skuString = forder.Sku.Replace(" ", "").Replace("\"", "").Replace("(", "").Replace(")", "").Replace(",", "").Replace(".", "");
                                var maxLength = 15;
                                if (skuString.Length > maxLength)
                                    skuString = skuString.Substring(0, maxLength);

                                forder.Article = production.Cells[x, 5].Value.ToString();
                                forder.MinOrder = "1";
                                forder.Group = production.Cells[x, 2].Value.ToString();
                                forder.FormingDate = "Последний день месяца";
                                forder.FormingTime = "23:00";
                                forder.DeliveryDate = "В последний день месяца";
                                forder.MaxOrder = production.Cells[x, 4].Value.ToString();
                                enabled = production.Cells[x, 3].Value.ToString();
                                typeFilter = "1";
                                break;
                        }



                        // фильтр для некоторых пользователей некоторых позиций
                        var skuFilter = "1";

                        var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                        var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                        // 36-Кирик, 72-Розница Обводный
                        //var userIds = new List<int> { 36, 72};
                        //if (userIds.Contains(user.Id) & forder.Group == "Гастрономия")
                        //skuFilter = "0";

                        // применение фильтров
                        if (enabled == "1" & typeFilter == "1" & skuFilter == "1")
                            OrdersTable.Add(forder);
                    }
                }
                var ordersByGroup = OrdersTable.Where(o => o.Group == group & o.DeliveryDate == delivery).ToList();

                ordersView.items = ordersByGroup;

                return PartialView(ordersView);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Сохранить заказ
        [HttpPost]
        public IActionResult SaveOrder(string json)
        {
            var result = new RKNet_Model.Result<string>();

            try
            {
                json = json.Replace("pp", "+");
                var order = JsonConvert.DeserializeObject<order>(json);

                // проверка на выбор ТТ
                if (order.tt == null)
                {
                    result.Ok = false;
                    result.Data = "Выберите торговую точку, на которую оформляется заказ.";
                    return new ObjectResult(result);
                }

                var tt = db.TTs.FirstOrDefault(t => t.Id == int.Parse(order.tt));

                // проверка на ввод количества
                foreach (var item in order.items)
                {
                    if (item.count == null)
                    {
                        result.Ok = false;
                        result.Data = "Не указано количество для позиции " + item.name;
                        return new ObjectResult(result);
                    }
                }

                // проверка даты доставки                
                if (order.type == 1) // регулярный заказ
                {

                    if (order.date == "")
                    {
                        result.Ok = false;
                        result.Data = "Не выбрана дата доставки заказа";
                        return new ObjectResult(result);
                    }
                    else
                    {
                        // учитываем время, в которое был сделан заказ               
                        bool timeOk = false;

                        var time = DateTime.Now.ToString("HH:mm");
                        var hour = int.Parse(time.Substring(0, 2));
                        var minute = int.Parse(time.Substring(3, 2));

                        var orderDate = DateTime.ParseExact(order.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                        var days = 0;

                        foreach (var item in order.items)
                        {
                            var itemDays = 0;
                            var maxTime = item.formingTime;

                            maxTime = maxTime.Substring(3, maxTime.Length - 3);

                            var maxHour = int.Parse(maxTime.Substring(0, 2));
                            var maxMinute = int.Parse(maxTime.Substring(3, 2));

                            if (hour < maxHour) timeOk = true;
                            if (hour == maxHour & minute < maxMinute) timeOk = true;

                            if (!timeOk)
                            {
                                itemDays = 1;
                            }

                            if (item.delivery == "Завтра") itemDays += 1;
                            if (item.delivery == "Послезавтра") itemDays += 2;
                            if (item.delivery == "За_З_дня") itemDays += 3;

                            if (days < itemDays) days = itemDays;
                        }

                        var deliveryDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(days);
                        var differense = orderDate.Subtract(deliveryDate).TotalDays;

                        if (differense < 0 & !User.IsInRole("forders_anydeliverydate"))
                        {
                            result.Ok = false;
                            result.Data = "Данный заказ невозможно выполнить ранее " + deliveryDate.ToString("dd.MM.yyyy") + " Пожалуйста, измените дату доставки.";
                            return new ObjectResult(result);
                        }
                    }
                }
                else // еженедельный или ежемесячный заказ
                {
                    var today = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

                    var nextMonthDays = DateTime.DaysInMonth(today.Year, today.Month) - today.Day + 1;
                    var nextWeekDays = 8 - (int)today.DayOfWeek;
                    var nextPeriod = today;

                    switch (order.type)
                    {
                        case 2:
                            nextPeriod = today.AddDays(nextWeekDays);
                            if (today.DayOfWeek == DayOfWeek.Monday & DateTime.Now.Hour < 12)
                            {
                                nextPeriod = nextPeriod.AddDays(-7);
                            }
                            break;
                        case 3:
                        case 4:
                            nextPeriod = today.AddDays(nextMonthDays);
                            break;
                    }

                    var existOrders = mssql.FranchOrders.Where(o => o.TTCode == tt.Code & o.OrderTypeId == order.type & o.DeliveryDate >= nextPeriod).Count();
                    if (existOrders > 0 & order.mode != "edit")
                    {
                        result.Ok = false;
                        result.Data = "По выбранной ТТ уже есть действующий заказ на следующий период. Вы можете изменить или удалить его, либо ожидать доставки.";
                        return new ObjectResult(result);
                    }
                }

                // размещение заказа в БД                
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                // исключаем дублирование строк с SKU перед сохранением (режим редактирования и возможные ошибки)
                var orderRange = mssql.FranchOrders.Where(f => f.OrderNumber == order.number);
                if (orderRange != null)
                    mssql.FranchOrders.RemoveRange(orderRange);


                var orderType = db.OrderTypes.FirstOrDefault(t => t.Id == order.type);

                int obd = db.TTs.FirstOrDefault(c => c.Restaurant_Sifr == tt.Restaurant_Sifr).Obd;

                List<string> articles = order.items.Select(item => item.article).ToList();


                List<ShipmentByGP> shipmentByGPs = reports1CSql.ShipmentsByGP.Where(x => x.DateOfShipmentChange >= DateTime.Now.AddDays(-30) && x.ConsigneeCodeN == obd && articles.Contains(x.Article)).ToList();

                foreach (var item in order.items)
                {
                    var franchOrder = new Models.MSSQL.FranchOrder();
                    franchOrder.OrderNumber = order.number;
                    franchOrder.OrderName = order.name;
                    franchOrder.TTName = tt.Name;
                    franchOrder.TTCode = tt.Code;
                    franchOrder.TTOBD = tt.Obd;
                    franchOrder.Article = item.article;
                    franchOrder.SKU = item.name;
                    franchOrder.minCount = double.Parse(item.mincount.Replace(".",","));
                    franchOrder.Count = double.Parse(item.count.Replace(".", ","));
                    if (orderType.Id == 1)
                        franchOrder.maxTime = item.formingTime.Substring(3, item.formingTime.Length - 3);
                    franchOrder.FormingDateTime = DateTime.Now;
                    franchOrder.minDeliveryDate = item.delivery;
                    franchOrder.DeliveryDate = DateTime.ParseExact(order.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    franchOrder.UserID = user.Id;
                    franchOrder.UserName = user.Name;
                    franchOrder.OrderTypeId = orderType.Id;
                    franchOrder.OrderTypeName = orderType.Name;

                    var shipmentByGP = shipmentByGPs.OrderByDescending(c => c.DateOfShipmentChange).FirstOrDefault(c => c.Article == item.article);

                    if (shipmentByGP != null)
                    {
                     franchOrder.LastPrice = shipmentByGP.OrderPrice / shipmentByGP.Quantity;
                    }
                    mssql.FranchOrders.Add(franchOrder);
                }

                // удаление старых заказов
                //var oldDate = DateTime.Now.AddDays(-30);
                //var old = mssql.FranchOrders.Where(f => f.DeliveryDate <= oldDate);
                //mssql.FranchOrders.RemoveRange(old);

                mssql.SaveChanges();
                result.Data = "Заказ успешно размещен.";

                // логируем
                var log = new LogEvent<int>(User);
                log.Name = "Сохранение заказа";
                log.Description = orderType.Name;
                log.Object = order.number;
                log.IpAdress = HttpContext.Session.GetString("ip");
                log.Save();

                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
                return new ObjectResult(result);
            }
        }

        // Удалить заказ
        public IActionResult DeleteOrder(int orderNumber)
        {
            var result = new RKNet_Model.Result<string>();

            try
            {
                var rangeToRemove = mssql.FranchOrders.Where(o => o.OrderNumber == orderNumber);
                mssql.FranchOrders.RemoveRange(rangeToRemove);
                mssql.SaveChanges();

                // логируем
                var log = new LogEvent<int>(User);
                log.Name = "Удаление заказа";
                log.Description = "/Orders/DeleteOrder";
                log.Object = orderNumber;
                log.IpAdress = HttpContext.Session.GetString("ip");
                log.Save();

                return new ObjectResult(result);
            }
            catch (Exception e)
            {
                result.Ok = false;
                result.Data = e.ToString();
                return new ObjectResult(result);
            }
        }


        //------------------------------------------------------------------------------------

        // класс для десериализации данных заказа с формы
        public class order
        {
            public int number;
            public string tt;
            public string name;
            public string date;
            public List<item> items;
            public string mode;
            public int type;

            public class item
            {
                public string name;
                public string article;
                public string count;
                public string mincount;
                public string delivery;
                public string formingTime;
            }
        }
    }


}
