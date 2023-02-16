using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Portal.Models.MSSQL;

namespace Portal.Controllers
{
    [Authorize]
    public class UserRequestsController : Controller
    {
        DB.SQLiteDBContext db;
        DB.MSSQLDBContext mssql;

        public UserRequestsController(DB.SQLiteDBContext sqliteContext, DB.MSSQLDBContext mssqlContext)
        {
            db = sqliteContext;
            mssql = mssqlContext;
        }

        // Список запросов
        [Authorize(Roles = ("settings"))]
        public IActionResult ReqList(string state, string selectedPage, string logsOnPage)
        {
            var reqList = new ViewModels.UserRequests.RequestsListView();

            // фильтр статусов
            if (state != null)
            {
                reqList.stateFilter = state;
            }

            // выбранная страница
            if (selectedPage != null)
            {
                reqList.selectedPage = int.Parse(selectedPage);
            }
            // кол-во строк данных на странице
            if (logsOnPage != null)
            {
                reqList.rowsOnPage = int.Parse(logsOnPage);
            }            


            // получаем отфильтрованные данные из БД и разбиваем на страницы
            reqList.countRows = mssql.UserRequests.Where(r => r.State.Contains(reqList.stateFilter)).Count();

            if (reqList.countRows > reqList.rowsOnPage)
            {
                reqList.requests = mssql.UserRequests.Where(r => r.State.Contains(reqList.stateFilter)).OrderBy(date => date.DateTime).Skip(reqList.countRows - reqList.rowsOnPage * reqList.selectedPage).Take(reqList.rowsOnPage).ToList();
            }
            else
            {
                reqList.requests = mssql.UserRequests.Where(r => r.State.Contains(reqList.stateFilter)).ToList();
            }


            return PartialView(reqList);
        }

        // Изменить статус запроса
        [Authorize(Roles = ("settings"))]
        public IActionResult ChangeState(int id, string newState)
        {
            try
            {
                var userReq = mssql.UserRequests.FirstOrDefault(r => r.Id == id);
                userReq.State = newState;
                mssql.SaveChanges();

                return new ObjectResult("ok");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        //------------------------------------------------------------------------------------------------
        // Запрос доступа
        public IActionResult RequestAccess(string roleCode)
        {
            try
            {
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == login.ToLower());
                var role = db.Roles.FirstOrDefault(r => r.Code == roleCode);

                var userReq = new UserRequest();
                userReq.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                userReq.UserId = user.Id;
                userReq.UserName = user.Name;
                userReq.UserJobTitle = user.JobTitle;
                userReq.Type = "Запрос доступа";
                userReq.RoleId = role.Id;
                userReq.RoleCode = role.Code;
                userReq.RoleName = role.Name;
                userReq.State = "new";

                mssql.UserRequests.Add(userReq);
                mssql.SaveChanges();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }
        
        // Отмена запроса доступа        
        public IActionResult CancelRequest(int id)
        {
            try
            {
                var userReq = mssql.UserRequests.FirstOrDefault(r => r.Id == id);
                mssql.UserRequests.Remove(userReq);
                mssql.SaveChanges();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        //------------------------------------------------------------------------------------------------
        // Уведомления
        [Authorize(Roles = ("settings"))]
        public IActionResult Alerts()
        {
            var requests = mssql.UserRequests.Where(r => r.State == "new").OrderByDescending(r => r.DateTime).ToList();
            return new ObjectResult(requests);
        }
    }
}
