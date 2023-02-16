using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using RKNet_Model.TT;

namespace Portal.Controllers
{
    [Authorize(Roles = "salesprediction")]
    public class SalesPredictionController : Controller
    {
        DB.MSSQLDBContext mssql;
        DB.SQLiteDBContext db;

        public SalesPredictionController(DB.SQLiteDBContext sqliteContext, DB.MSSQLDBContext mssqlContext)
        {
            db = sqliteContext;
            mssql = mssqlContext;
        }

        public IActionResult PredictionTable(int monthCount)
        {
            try
            {
                var predView = new ViewModels.PredictionsViewModel();
                var login = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.WindowsAccountName).Value;
                var user = db.Users.Include(u => u.TTs).FirstOrDefault(u => u.Login.ToLower() == login.ToLower());

                predView.TTs = user.TTs.Where(t => !t.Closed).OrderBy(t => t.Name).ToList();

                predView.Predictions = mssql.SalesPredictions.ToList();
                predView.MonthCount = monthCount;

                return PartialView(predView);
            }
            catch(Exception e)
            {
                return new ObjectResult(e.ToString());
            }
        }

        // Запись в БД
        public IActionResult SavePrediction(string userLogin, string tId, string date, int value)
        {
            try
            {                
                var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == userLogin.ToLower());
                var dateTime = date.Replace("-", ".");
                

                List<TT> tts;

                switch (tId)
                {
                    case "sleeping":
                        tts = db.TTs.Where(t => t.Type.Name == "Спальник").ToList();                        
                        break;

                    case "centr":
                        tts = db.TTs.Where(t => t.Type.Name == "Центр").ToList();
                        break;

                    default:
                        tts = db.TTs.Where(t => t.Id == int.Parse(tId)).ToList();
                        break;
                }

                foreach (var tt in tts)
                {

                    var prediction = new Models.MSSQL.SalesPrediction();

                    prediction.Date = dateTime;
                    prediction.PredictionValue = value;
                    prediction.UserId = user.Id;
                    prediction.UserName = user.Name;

                    prediction.TTCode = tt.Code;
                    prediction.TTName = tt.Name;

                    var data = mssql.SalesPredictions.Where(p => p.TTCode == prediction.TTCode & p.Date == prediction.Date);

                    if (data.Count() > 0)
                    {
                        var oldPrediction = data.FirstOrDefault();
                        if (value > 0)
                        {
                            oldPrediction.TTName = prediction.TTName;
                            oldPrediction.UserId = prediction.UserId;
                            oldPrediction.UserName = prediction.UserName;
                            oldPrediction.PredictionValue = prediction.PredictionValue;
                        }
                        else
                        {
                            mssql.SalesPredictions.Remove(oldPrediction);
                        }
                        mssql.SaveChanges();
                    }
                    else
                    {
                        if (value > 0)
                        {
                            mssql.SalesPredictions.Add(prediction);
                            mssql.SaveChanges();
                        }
                    }

                }


                return new ObjectResult("Ok");
            }
            catch(Exception e)
            {
                return new ObjectResult(e.ToString());
            }
            
        }
    }
}
