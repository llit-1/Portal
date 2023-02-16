using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Portal.ViewModels.SkuStop;
using Newtonsoft.Json;
using Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Portal.Controllers
{
    [Authorize(Roles = "skustop")]
    public class SkuStopController : Controller
    {
        DB.SQLiteDBContext db;
        DB.MSSQLDBContext mssql;

        public SkuStopController(DB.SQLiteDBContext sqliteContext, DB.MSSQLDBContext mssqlContext)
        {
            db = sqliteContext;
            mssql = mssqlContext;
        }
        // список заблокированных блюд
        public IActionResult Index(string selectedPage, string rowsOnPage, string createDate, string userName, string state, string finished)
        {
            var indexView = new IndexView();
            try
            {
                // выбранная страница
                if (selectedPage != null)
                {
                    indexView.selectedPage = int.Parse(selectedPage);
                }
                // количество строк на странице
                if (rowsOnPage != null)
                {
                    indexView.rowsOnPage = int.Parse(rowsOnPage);
                }
                // фильтр даты отправки
                if (createDate != null)
                {
                    indexView.createDate = createDate;
                    createDate = DateTime.ParseExact(indexView.createDate, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                }
                else
                {
                    createDate = "";
                }
                // фильтр пользователей
                if (userName != null)
                {
                    userName = userName.Replace("pp", "+");
                    userName = userName.Replace("bb", " ");
                    indexView.userName = userName;
                }
                // фильтр статусов
                if (state != null)
                {
                    state = state.Replace("pp", "+");
                    state = state.Replace("bb", " ");
                    indexView.state = state;
                }

                // фильтр завершённых блокировок
                var today = DateTime.Now;
                if (finished != null)
                {
                    indexView.finished = finished;  
                    if (finished == "1")
                    {
                        today = DateTime.MinValue;
                    }
                }       
                else
                {
                    today = DateTime.MinValue;
                }
                                

                // получаем данные для фильтров
                indexView.createdDates = mssql.SkuStops.Select(d => d.Created).OrderByDescending(d => d).Select(d => d.ToString("dd.MM.yyyy")).ToList().Distinct().ToList();
                indexView.userNames = mssql.SkuStops.Select(u => u.UserName).Where(u => u.Length > 0).Distinct().ToList();
                indexView.states = mssql.SkuStops.Select(s => s.State).Where(s => s.Length > 0).Distinct().ToList();                

                // получаем отфильтрованные данные из БД и разбиваем на страницы
                indexView.countRows = mssql.SkuStops
                    .Where(s => s.Created.ToString().Contains(createDate) & s.UserName.Contains(indexView.userName) & s.State.Contains(indexView.state) & (s.UserCancelDate.Value.Date == today.Date | s.Expiration.Date >= today.Date))
                    .Count();

                var skipRows = indexView.countRows - (indexView.rowsOnPage * indexView.selectedPage);
                if (skipRows < 0)
                    skipRows = 0;

                indexView.stopList = mssql.SkuStops
                        .Where(s => s.Created.ToString().Contains(createDate) & s.UserName.Contains(indexView.userName) & s.State.Contains(indexView.state) & (s.UserCancelDate.Value.Date == today.Date | s.Expiration.Date >= today.Date))
                        .OrderBy(date => date.Created)
                        .Skip(skipRows).Take(indexView.rowsOnPage).ToList();

                

                return PartialView(indexView);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }
        
        // форма блокировки
        public IActionResult AddBlock()
        {
            var addView = new ViewModels.SkuStop.AddBlockView();

            // получаем данные пользователя
            var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.Include(u => u.TTs).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            // все тт
            if (User.IsInRole("skustop_alltt"))
            {
                addView.TTs = db.TTs.Include(c => c.CashStations).Where(t => !t.Closed).Where(t => t.CashStations.Count > 0).ToList();
            }
            // тт пользователя
            else
            {
                addView.TTs = db.TTs
                .Include(c => c.CashStations)
                .Include(t => t.Users)
                .Where(t => !t.Closed)
                .Where(t => t.CashStations.Count > 0)
                .Where(t => t.Users.Contains(user))
                .ToList();
            }                                   

            // получаем меню Р-Кипер
            var menuRkResult = new RKNet_Model.Rk7XML.Response.GetMenuResponse.RK7QueryResult();

            try
            {
                // Создание экземпляра класса запроса XML
                var xml = new RKNet_Model.Rk7XML.Request.GetMenu.GetRefData();
                var xmlRequest = RKNet_Model.Rk7XML.Request.Serialize.ToString(xml);

                // Запрос и получение XML ответа от сервера справочников Р-Кипер             
                var rk = new RKNet_Model.Rk7XML.RK7();
                var rkSettings = db.RKSettings.FirstOrDefault();

                var responseResult = rk.SendRequest(rkSettings.RefServerIp, xmlRequest, rkSettings.RefServerPort, rkSettings.User, rkSettings.Password);

                // Разбор полученного XML
                if (responseResult.Ok)
                {
                    menuRkResult = RKNet_Model.Rk7XML.Response.GetMenuResponse.RK7QueryResult.DeSerializeQueryResult(responseResult.Data);

                    // рекурсивный перебор Меню Р-Кипер и формирование Меню для Combo-Tree
                    var menu = new List<MenuItem>();
                    foreach (var rkCat in menuRkResult.rk7Reference.rIChildItems.tCategListItemList)
                    {
                        var category = new MenuItem();
                        category.isSelectable = false;
                        category.title = rkCat.Name;
                        category.id = rkCat.Code;
                        category.subs = GetItems(rkCat);

                        menu.Add(category);
                    }

                    addView.MenuItems = menu;
                }
                else
                {
                    return new ObjectResult("ошибка запроса меню Р-Кипер: " + responseResult.ErrorMessage);
                }
            }
            catch (Exception e)
            {
                return new ObjectResult("ошибка запроса меню Р-Кипер: " + e.ToString());
            }
            
            return PartialView(addView);
        }

        // форма разблокировки кассы
        public IActionResult CashUnlock()
        {
            var cashList = db.CashStations.Include(c => c.TT).Where(c => !c.TT.Closed).ToList();
            return PartialView(cashList);
        }

        // отправляем разблокировку на кассу
        public IActionResult SendUnlock(int cashId)
        {
            var result = new RKNet_Model.Result<string>();
            if(cashId == 0)
            {
                result.Ok = false;
                result.ErrorMessage = "получен неверный id кассы";
                return new ObjectResult(result);
            }
            
            try
            {
                var cash = db.CashStations.FirstOrDefault(c => c.Id == cashId);

                if(cash == null)
                {
                    result.Ok = false;
                    result.ErrorMessage = $"Кассы с Id = {cashId} не обнаружена в базе данных";
                    return new ObjectResult(result);
                }                    

                // отправка запроса
                var rkSettings = db.RKSettings.FirstOrDefault();

                // создаем экземпляра класса запроса XML
                var xml = new RKNet_Model.Rk7XML.Request.SetDishRests.RK7Query("111", false);
                var xmlRequest = RKNet_Model.Rk7XML.Request.Serialize.ToString(xml);

                result.Data = xmlRequest;
                return new ObjectResult(result);

                var rk = new RKNet_Model.Rk7XML.RK7();
                //result = rk.SendRequest(cash.Ip, request, rkSettings.CashPort, rkSettings.User, rkSettings.Password);

                if(result.Ok)
                {
                    result.Data = "Касса разблокирована";
                }                
            }
            catch(Exception ex)
            {
                result.Ok = false;
                result.ErrorMessage = ex.Message;
                result.ExceptionText = ex.ToString();
            }
            
            return new ObjectResult(result);
        }

        // запись стопа в БД
        public IActionResult SaveBlock(string json)
        {
            try
            {
                json = json.Replace("pp", "+");
                var stopView = JsonConvert.DeserializeObject<ViewModels.SkuStop.AddBlockView>(json);

                // проверка введенных данных
                if (DateTime.Now.TimeOfDay.Hours >= 23 | DateTime.Now.TimeOfDay.Hours <= 6)
                {
                    //return new ObjectResult("блокировать позиции можно в интервале от 07:00 до 23:00");
                }

                if (stopView.skuName.Length == 0)
                {
                    return new ObjectResult("не выбрана позиция для блокировки");
                }

                if (stopView.reason.Length == 0)
                {
                    return new ObjectResult("не указана причина блокировки позиции");
                }

                if (stopView.reason.Length > 100)
                {
                    return new ObjectResult("превышен максимальный размер текста причины блокировки");
                }

                DateTime expiration;
                var isExpireOk = DateTime.TryParse(stopView.expiration, out expiration);
                
                if (!isExpireOk)
                {
                    return new ObjectResult("некорректно заполнен период блокировки");
                }
                else
                {
                    if(expiration <= DateTime.Now)
                        return new ObjectResult("дата и время блокировки не могут быть указаны раньше текущего времени");
                }

                if (stopView.ttIds.Count == 0)
                {
                    return new ObjectResult("выберите торговые точки для блокировки позиции");
                }

                // данные пользователя
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                // данные касс по тт
                var skuStopStates = new List<RKNet_Model.MSSQL.SkuStopState>();
                var cashMsgStates = new List<Models.MSSQL.CashMsgState>();

                foreach (var id in stopView.ttIds)
                {
                    var tt = db.TTs.Include(c => c.CashStations).FirstOrDefault(t => t.Id == id);
                    foreach (var cash in tt.CashStations)
                    {
                        var skuStopState = new RKNet_Model.MSSQL.SkuStopState
                        {
                            TTId = tt.Id,
                            TTCode = tt.Code,
                            TTName = tt.Name,
                            cashId = cash.Id,
                            CashName = cash.Name,
                            blocked = false,
                            error = null
                        };
                        skuStopStates.Add(skuStopState);

                        var cashMsgState = new Models.MSSQL.CashMsgState
                        {
                            TTId = tt.Id,
                            TTCode = tt.Code,
                            TTName = tt.Name,
                            cashId = cash.Id,
                            CashName = cash.Name,
                            sended = false,
                            error = null
                        };
                        cashMsgStates.Add(cashMsgState);
                    }
                }

                // запись данных в бд
                var skuStop = new RKNet_Model.MSSQL.SkuStop();
                skuStop.SkuName =stopView.skuName;
                skuStop.SkuRkCode = stopView.skuRkCode;
                skuStop.Reason = stopView.reason;
                skuStop.UserName = user.Name;
                skuStop.UserId = user.Id;
                skuStop.Created = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                skuStop.Expiration = expiration;
                skuStop.CashStates = JsonConvert.SerializeObject(skuStopStates);
                skuStop.State = "ожидает отправки";
                skuStop.Finished = "0";

                // отправка сообщения на кассы о блокировке позиции
                var cashMessage = new Models.MSSQL.CashMessage();
                cashMessage.Name = "СТОП";
                cashMessage.Text = "СТОП-ЛИСТ: " + skuStop.SkuName + ". Причина: " + skuStop.Reason;
                cashMessage.UserName = user.Name;
                cashMessage.UserId = user.Id;
                cashMessage.Created = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                cashMessage.Expiration = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 00, 00);
                cashMessage.CashMsgStates = JsonConvert.SerializeObject(cashMsgStates);
                cashMessage.State = "ожидает отправки";
                cashMessage.Finished = "0";

                mssql.CashMessages.Add(cashMessage);
                mssql.SkuStops.Add(skuStop);
                mssql.SaveChanges();

                // логируем
                var log = new LogEvent<string>(User);
                log.Name = "блокировка позиции на кассах";
                log.Description = skuStop.SkuName;
                log.IpAdress = HttpContext.Session.GetString("ip");
                log.Save();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // отмена блокировки вручную
        public IActionResult CancelBlock(int stopId)
        {
            var stop = mssql.SkuStops.FirstOrDefault(s => s.Id == stopId);
            var today = DateTime.Now;
            var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
            var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

            stop.Canceled = true;
            stop.UserCancel = true;
            stop.UserCancelDate = new DateTime(today.Year, today.Month, today.Day, today.Hour, today.Minute, today.Second);
            stop.UserCancelName = user.Name;
            stop.UserCancelId = user.Id;
            stop.State = "ожидает отправки";
            
            mssql.SkuStops.Update(stop);
            mssql.SaveChanges();

            // логируем
            var log = new LogEvent<string>(User);
            log.Name = "отмена стопа на кассах";
            log.Description = stop.SkuName;
            log.IpAdress = HttpContext.Session.GetString("ip");
            log.Save();

            return new ObjectResult("ok");
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // метод рекурсивного перебора элементов меню Р-Кипер
        private List<MenuItem> GetItems(RKNet_Model.Rk7XML.Response.GetMenuResponse.TCategListItem rkCategory)
        {
            var items = new List<MenuItem>();
            foreach(var rkItem in rkCategory.childItems.MenuItems)
            {
                var item = new MenuItem();
                item.isSelectable = true;
                item.title = rkItem.Name;
                item.id = rkItem.Code;              

                items.Add(item);
            }

            foreach (var rkCat in rkCategory.childItems.tCategListItemList)
            {
                var category = new MenuItem();
                category.isSelectable = false;
                category.title = rkCat.Name;
                category.id = rkCat.Code;
                category.subs = GetItems(rkCat);

                items.Add(category);
            }

            return items;
        }
    }
}
