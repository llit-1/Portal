using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Portal.HostedServices
{
    public class SkuStopService : IHostedService
    {
        //--------------------------------------------------------------------------------------
        // Служба отправки сообщений на кассы
        //--------------------------------------------------------------------------------------

        Portal.Models.Settings.Module module;

        protected IServiceProvider serviceProvider;
        Timer serviceTimer;
        private readonly ILogger _log;

        // Конструктор
        public SkuStopService(IServiceProvider services, ILogger<SkuStopService> log)
        {
            serviceProvider = services;
            _log = log;
        }

        // Запуск службы
        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                try
                {
                    var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                    module = sqlite.Modules.FirstOrDefault(m => m.Name == "SkuStop");
                }
                catch (Exception e)
                {
                    _log.LogError(e.Message);
                }
            }

            serviceTimer = new Timer(GetData, null, 0, 60000 * int.Parse(module.Interval));

            if (module.Enabled)
            {
                _log.LogInformation("модуль запущен");
            }
            else
            {
                serviceTimer.Change(Timeout.Infinite, 0);
                _log.LogInformation("модуль отключен");
            }

            return Task.CompletedTask;
        }

        // Остановка службы
        public Task StopAsync(CancellationToken cancellationToken)
        {
            serviceTimer.Change(Timeout.Infinite, 0);
            _log.LogInformation("модуль остановлен");
            return Task.CompletedTask;
        }

        //--------------------------------------------------------------------------------------
        // Методы 
        //--------------------------------------------------------------------------------------

        void GetData(object state)
        {
            if (module.Enabled)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    try
                    {
                        var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                        var mssql = (DB.MSSQLDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.MSSQLDBContext));

                        var module = sqlite.Modules.FirstOrDefault(m => m.Name == "SkuStop");

                        // получаем список id активных стопов из БД
                        var stopIds = mssql.SkuStops.Where(s => s.Finished == "0").Select(s => s.Id).ToList();
                        
                        foreach (var stopId in stopIds)
                        {
                            var curStop = mssql.SkuStops
                                .Select(s => new 
                                { 
                                    Id = s.Id, 
                                    Expiration = s.Expiration,
                                    CashStates = s.CashStates,
                                    UserCancel = s.UserCancel,
                                    SkuRkCode = s.SkuRkCode,
                                    CancelMsg = s.CancelMsg
                                })
                                .FirstOrDefault(s => s.Id == stopId);
                            
                            var cashStates = JsonConvert.DeserializeObject<List<RKNet_Model.MSSQL.SkuStopState>>(curStop.CashStates);
                           
                            // установка стопов
                            if (DateTime.Now < curStop.Expiration & !curStop.UserCancel)
                            {
                                //_log.LogInformation("....................................................... TEST-TEST-TEST" + curStop.Expiration);
                                foreach (var cashState in cashStates)//.Where(c => !c.blocked))
                                {
                                    var cash = sqlite.CashStations.FirstOrDefault(c => c.Id == cashState.cashId);
                                    _log.LogInformation(".......................................................отправка стопа на кассу " + cash.Name + " (" + cash.Ip + ")...");

                                    var result = SetStop(cash.Ip, curStop.SkuRkCode, true);
                                    if (result.Ok)
                                    {
                                        cashState.blocked = true;
                                        cashState.error = null;
                                        _log.LogInformation(".......................................................Ok");
                                    }
                                    else
                                    {
                                        //cashState.blocked = false;
                                        cashState.error = result.ErrorMessage;
                                        _log.LogError(".......................................................Ошибка: " + result.ErrorMessage);
                                    }

                                    // обновляем текущий стоп после каждой отправки команды на касу
                                    var stop = mssql.SkuStops.FirstOrDefault(s => s.Id == curStop.Id);
                                    stop.CashStates = JsonConvert.SerializeObject(cashStates);

                                    var totalCashes = cashStates.Count();
                                    var blockedCashes = cashStates.Where(c => c.blocked).Count();

                                    if (totalCashes == blockedCashes)
                                        stop.State = "заблокировано";
                                    else
                                        stop.State = "блокировка...";

                                    mssql.SkuStops.Update(stop);
                                    mssql.SaveChanges();
                                }                                
                            }
                            // снятие стопов
                            else
                            {
                                // снятие стопов вручную
                                if (curStop.UserCancel)
                                {
                                    // отправляем сообщение на все кассы о снятии позиции с блокировки
                                    var CancelMsg = curStop.CancelMsg;
                                    if (!CancelMsg)
                                    {
                                        CashMessage(mssql.SkuStops.First(s => s.Id == curStop.Id));
                                        CancelMsg = true;
                                    }
                                    
                                    foreach (var cashState in cashStates.Where(c => c.blocked))
                                    {
                                        var cash = sqlite.CashStations.FirstOrDefault(c => c.Id == cashState.cashId);
                                        var result = SetStop(cash.Ip, curStop.SkuRkCode, false);
                                        if (result.Ok)
                                        {
                                            cashState.blocked = false;
                                            cashState.error = null;                                            
                                            _log.LogInformation(".......................................................блокировка по кассе " + cash.Name + " снята");
                                        }
                                        else
                                        {
                                            cashState.error = result.ErrorMessage;
                                            _log.LogError(".......................................................ошибка снятия блокировки по кассе " + cash.Name + ": " + result.ErrorMessage);
                                        }

                                        // обновляем текущий стоп после каждой отправки команды на касу
                                        var stop = mssql.SkuStops.FirstOrDefault(s => s.Id == curStop.Id);
                                        stop.CashStates = JsonConvert.SerializeObject(cashStates);

                                        // меняем общий статус стопа
                                        var blockedCashes = cashStates.Where(c => c.blocked).Count();

                                        if (blockedCashes == 0)
                                        {
                                            stop.State = "отменено";
                                            stop.Finished = "1";
                                        }
                                        else
                                            stop.State = "отмена стопов...";

                                        stop.CancelMsg = CancelMsg;

                                        mssql.SkuStops.Update(stop);
                                        mssql.SaveChanges();
                                    }                                    
                                }
                                // снятие стопов по истечению времени
                                else
                                {
                                    var Canceled = true;
                                    var CancelMsg = curStop.CancelMsg;

                                    // отправляем сообщение на все кассы о снятии позиции с блокировки
                                    if (!CancelMsg)
                                    {
                                        CashMessage(mssql.SkuStops.First(s => s.Id == curStop.Id));
                                        CancelMsg = true;
                                    }

                                    foreach (var cashState in cashStates.Where(c => c.blocked))
                                    {
                                        var cash = sqlite.CashStations.FirstOrDefault(c => c.Id == cashState.cashId);
                                        var result = SetStop(cash.Ip, curStop.SkuRkCode, false);
                                        if (result.Ok)
                                        {
                                            cashState.blocked = false;
                                            cashState.error = null;
                                            _log.LogInformation(".......................................................блокировка по кассе " + cash.Name + " снята");
                                        }
                                        else
                                        {
                                            cashState.error = result.ErrorMessage;
                                            _log.LogError(".......................................................ошибка снятия блокировки по кассе " + cash.Name + ": " + result.ErrorMessage);
                                        }

                                        // обновляем текущий стоп после каждой отправки команды на касу
                                        var stop = mssql.SkuStops.FirstOrDefault(s => s.Id == curStop.Id);
                                        stop.CashStates = JsonConvert.SerializeObject(cashStates);

                                        // меняем общий статус стопа
                                        var blockedCashes = cashStates.Where(c => c.blocked).Count();

                                        if (blockedCashes == 0)
                                        {
                                            stop.State = "завершено";
                                            stop.Finished = "1";
                                        }
                                        else
                                        {
                                            stop.State = "отмена стопов...";
                                        }

                                        stop.CancelMsg = CancelMsg;
                                        stop.Canceled = Canceled;

                                        mssql.SkuStops.Update(stop);
                                        mssql.SaveChanges();
                                    }                                    
                                }
                                
                                // обновляем текущий статус (на случай если стопы на кассах не были применены)
                                var st = mssql.SkuStops.FirstOrDefault(s => s.Id == curStop.Id);
                                var blocked = cashStates.Where(c => c.blocked).Count();

                                if (blocked == 0 & st.Finished != "1")
                                {
                                    st.State = "отменено";
                                    st.Finished = "1";
                                    mssql.SkuStops.Update(st);
                                    mssql.SaveChanges();
                                }
                            }
                        }                      
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e.ToString());
                    }
                }
            }
        }

        // установка/снятие стопа
        RKNet_Model.Result<string> SetStop(string cashIp, string dishCode, bool stop)
        {
            var result = new RKNet_Model.Result<string>();
            using (var scope = serviceProvider.CreateScope())
            {
                try
                {
                    // получаем данные для подключения к кассе
                    var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                    var rkSettings = sqlite.RKSettings.FirstOrDefault();

                    // создаем экземпляра класса запроса XML
                    var xml = new RKNet_Model.Rk7XML.Request.SetDishRests.RK7Query(dishCode, stop);
                    var xmlRequest = RKNet_Model.Rk7XML.Request.Serialize.ToString(xml);

                    // отправляем запрос
                    var rk = new RKNet_Model.Rk7XML.RK7();
                    var reqResult = rk.SendRequest(cashIp, xmlRequest, rkSettings.CashPort, rkSettings.User, rkSettings.Password);

                    if (reqResult.Ok)
                    {
                        var xmlResponse = reqResult.Data;
                        
                        // дессериализация ответа
                        var rkResult = RKNet_Model.Rk7XML.Response.MainResponse.RK7QueryResult.DeSerializeQueryResult(xmlResponse);
                        if (rkResult.Status != "Ok")
                        {
                            result.Ok = false;
                            result.ErrorMessage = rkResult.ErrorText;
                        }
                    }
                    else
                    {
                        result.Ok = false;
                        result.ErrorMessage = reqResult.ErrorMessage;
                    }
                }
                catch (Exception e)
                {
                    result.Ok = false;
                    result.ErrorMessage = e.ToString();
                }
            }
            return result;
        }

        // сообщение на кассы о разблокировке позиции
        void CashMessage(RKNet_Model.MSSQL.SkuStop stop)
        {
            // данные касс по тт
            var cashMsgStates = new List<Models.MSSQL.CashMsgState>();
            var cashStates = JsonConvert.DeserializeObject<List<RKNet_Model.MSSQL.SkuStopState>>(stop.CashStates);

            using (var scope = serviceProvider.CreateScope())
            {
                var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                var mssql = (DB.MSSQLDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.MSSQLDBContext));

                foreach (var state in cashStates)
                {
                    var cashMsgState = new Models.MSSQL.CashMsgState
                    {
                        TTId = state.TTId,
                        TTCode = state.TTCode,
                        TTName = state.TTName,
                        cashId = state.cashId,
                        CashName = state.CashName,
                        sended = false,
                        error = null
                    };
                    cashMsgStates.Add(cashMsgState);                    
                }                

                // отправка сообщения на кассы о разблокировке позиции
                var cashMessage = new Models.MSSQL.CashMessage();
                cashMessage.Name = "Снятие со СТОПа";
                cashMessage.Text = "Позиция " + stop.SkuName + " снята со СТОПа.";
                if(stop.UserCancelName != null) cashMessage.UserName = stop.UserCancelName;
                if (stop.UserCancelId != null) cashMessage.UserId = stop.UserId;
                cashMessage.Created = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                cashMessage.Expiration = new DateTime(DateTime.Now.AddDays(1).Year, DateTime.Now.AddDays(1).Month, DateTime.Now.AddDays(1).Day, 23, 00, 00);
                cashMessage.CashMsgStates = JsonConvert.SerializeObject(cashMsgStates);
                cashMessage.State = "ожидает отправки";
                cashMessage.Finished = "0";

                mssql.CashMessages.Add(cashMessage);
                mssql.SaveChanges();
            }
        }
    }
}
