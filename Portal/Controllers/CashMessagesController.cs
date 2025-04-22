using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Portal.Models;
using Newtonsoft.Json;
using Portal.ViewModels.CashMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Portal.Models.MSSQL;
using System.Net.Http;

namespace Portal.Controllers
{
    [Authorize(Roles = "cashmsg")]
    public class CashMessagesController : Controller
    {
        DB.SQLiteDBContext db;
        DB.MSSQLDBContext mssql;

        public CashMessagesController(DB.SQLiteDBContext sqliteContext, DB.MSSQLDBContext mssqlContext)
        {
            db = sqliteContext;
            mssql = mssqlContext;
        }

        // Страница списка сообщений
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

                // фильтр завершённых сообщений
                if (finished != null)
                {
                    indexView.finished = finished;
                }
                
                // получаем данные для фильтров
                indexView.createdDates = mssql.CashMessages.Select(d => d.Created).OrderByDescending(d => d).Select(d => d.ToString("dd.MM.yyyy")).ToList().Distinct().ToList();                
                indexView.userNames = mssql.CashMessages.Select(u => u.UserName).Where(u => u.Length > 0).Distinct().ToList();
                indexView.states = mssql.CashMessages.Select(s => s.State).Where(s => s.Length > 0).Distinct().ToList();

                // получаем отфильтрованные данные из БД и разбиваем на страницы
                if (indexView.state != "")
                {
                    indexView.countRows = mssql.CashMessages
                        .Where(s => s.Created.ToString().Contains(createDate) & s.UserName.Contains(indexView.userName) & s.State == indexView.state & s.Finished.Contains(indexView.finished))
                        .Count();

                    var skipRows = indexView.countRows - (indexView.rowsOnPage * indexView.selectedPage);
                    if (skipRows < 0)
                        skipRows = 0;

                    indexView.cashMessages = mssql.CashMessages
                            .Where(s => s.Created.ToString().Contains(createDate) & s.UserName.Contains(indexView.userName) & s.State == indexView.state & s.Finished.Contains(indexView.finished))
                            .OrderBy(date => date.Created)
                            .Skip(skipRows).Take(indexView.rowsOnPage).ToList();
                }
                else
                {
                    indexView.countRows = mssql.CashMessages
                        .Where(s => s.Created.ToString().Contains(createDate) & s.UserName.Contains(indexView.userName) & s.State.Contains(indexView.state) & s.Finished.Contains(indexView.finished))
                        .Count();

                    var skipRows = indexView.countRows - (indexView.rowsOnPage * indexView.selectedPage);
                    if (skipRows < 0)
                        skipRows = 0;

                    indexView.cashMessages = mssql.CashMessages
                            .Where(s => s.Created.ToString().Contains(createDate) & s.UserName.Contains(indexView.userName) & s.State.Contains(indexView.state) & s.Finished.Contains(indexView.finished))
                            .OrderBy(date => date.Created)
                            .Skip(skipRows).Take(indexView.rowsOnPage).ToList();
                }

                return PartialView(indexView);
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }            
        }

        // Форма отправки сообщения на кассы
        public IActionResult InputMessage()
        {
            var tts = db.TTs.Include(c => c.CashStations).Where(t => !t.Closed).Where(t => t.CashStations.Count > 0).ToList();
            return PartialView(tts);
        }

        // Запись сообщения в БД
        public IActionResult SaveMessage(string json)
        {
            try
            {
                json = json.Replace("pp", "+");
                var cashMessageView = JsonConvert.DeserializeObject<ViewModels.CashMessages.CashMessageView>(json);
                
                // проверка введенных данных
                if (DateTime.Now.TimeOfDay.Hours >= 23 | DateTime.Now.TimeOfDay.Hours <= 6)
                {
                    return new ObjectResult("сообщения на кассы можно отправлять в интервале от 07:00 до 23:00");
                }

                if(cashMessageView.Name.Length == 0)
                {
                    return new ObjectResult("некорректный заголовок сообщения");
                }

                if(cashMessageView.Text.Length == 0)
                {
                    return new ObjectResult("заполните текст сообщения");
                }

                if (cashMessageView.Text.Length > 1000)
                {
                    return new ObjectResult("превышен максимальный размер сообщения");
                }

                if (cashMessageView.ttIds.Count == 0)
                {
                    return new ObjectResult("выберите точки для получения сообщения");
                }

                // данные пользователя
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                // данные касс по тт
                var cashMsgStates = new List<Models.MSSQL.CashMsgState>();
                
                foreach(var id in cashMessageView.ttIds)
                {
                    var tt = db.TTs.Include(c => c.CashStations).FirstOrDefault(t => t.Id == id);
                    foreach(var cash in tt.CashStations)
                    {
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

                //отправка на API для отправки на кассы через SignalR
                Message message = new Message();
                message.Date = DateTime.Now.ToString();
                message.Text = cashMessageView.Text;
                message.IsReading = false;
                MessageOrder messageOrder = new MessageOrder();
                messageOrder.Message = message;
                List<RKNet_Model.TT.TT> tTs = db.TTs.Include(x => x.CashStations)
                                                    .Where(x => cashMessageView.ttIds.Contains(x.Id))
                                                    .ToList();
                messageOrder.TTs = tTs;
                ApiRequest.SendMessage(messageOrder);

                // запись данных в бд
                var cashMessage = new Models.MSSQL.CashMessage();
                cashMessage.Name = cashMessageView.Name;
                cashMessage.Text = cashMessageView.Text;
                cashMessage.UserName = user.Name;
                cashMessage.UserId = user.Id;                
                cashMessage.Created = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                cashMessage.Expiration = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 00, 00);
                cashMessage.CashMsgStates = JsonConvert.SerializeObject(cashMsgStates);
                cashMessage.State = "ожидает отправки";
                cashMessage.Finished = "0";

                mssql.CashMessages.Add(cashMessage);
                mssql.SaveChanges();

                // логируем
                var log = new LogEvent<string>(User);
                log.Name = "Отправка сообщения на кассы";
                log.Description = cashMessageView.Name;
                log.IpAdress = HttpContext.Session.GetString("ip");
                log.Save();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }
    }
}
