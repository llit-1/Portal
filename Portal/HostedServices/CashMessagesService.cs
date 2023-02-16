using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RKNet_Model.VMS;
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
    public class CashMessagesService : IHostedService
    {
        //--------------------------------------------------------------------------------------
        // Служба отправки сообщений на кассы
        //--------------------------------------------------------------------------------------

        Portal.Models.Settings.Module module;

        protected IServiceProvider serviceProvider;
        Timer serviceTimer;
        private readonly ILogger _log;

        // Конструктор
        public CashMessagesService(IServiceProvider services, ILogger<CashMessagesService> log)
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
                    module = sqlite.Modules.FirstOrDefault(m => m.Name == "CashMessages");
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

                        var module = sqlite.Modules.FirstOrDefault(m => m.Name == "CashMessages");

                        // получаем список активных сообщений из БД
                        var workMessages = mssql.CashMessages.Where(m => m.Finished == "0");                        
                        
                        // закрываем сообщения, цикл жизни которых завершился
                        foreach(var msg in workMessages)
                        {
                            if(DateTime.Now >= msg.Expiration)
                            {
                                // подсчитываем количество касс, получивших сообщение
                                var cashStates = JsonConvert.DeserializeObject<List<Models.MSSQL.CashMsgState>>(msg.CashMsgStates);
                                
                                var totalCashes = cashStates.Count();
                                var sendedCashes = cashStates.Where(c => c.sended).Count();

                                if (totalCashes == sendedCashes)
                                    msg.State = "доставлено";
                                if (totalCashes > sendedCashes & sendedCashes > 0)
                                    msg.State = "частично доставлено";
                                if (sendedCashes == 0)
                                    msg.State = "не доставлено";

                                msg.Finished = "1";
                            }
                        }                        

                        mssql.CashMessages.UpdateRange(workMessages);
                        mssql.SaveChanges();

                        // отправляем сообщения на кассы
                        workMessages = mssql.CashMessages.Where(m => m.Finished == "0");

                        if (workMessages.Count() > 0)
                            _log.LogInformation(".......................................................рассылка сообщений на кассы, всего активных: " + workMessages.Count());

                        //var cashes = sqlite.CashStations.ToList();
                        var RK = sqlite.RKSettings.FirstOrDefault();

                        foreach (var msg in workMessages)
                        {
                            var cashStates = JsonConvert.DeserializeObject<List<Models.MSSQL.CashMsgState>>(msg.CashMsgStates);
                            foreach (var cashState in cashStates.Where(s => !s.sended))
                            {
                                var cash = sqlite.CashStations.FirstOrDefault(c => c.Id == cashState.cashId);

                                _log.LogInformation(".......................................................отправка на кассу " + cash.Name + " (" + cash.Ip + ")...");

                                // Параметры подключения к кассе                                
                                string ip = cash.Ip;
                                string port = RK.CashPort;
                                string user = RK.User;
                                string password = RK.Password;

                                var MessageText = msg.Text.Replace("\"", "&quot;");

                                var ExpireTime = msg.Expiration.ToString("yyyy-MM-ddTHH:mm:ss");
                                //var WaiterCode = "10";
      
                                var xml_request = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><RK7Query><RK7CMD CMD=\"WaiterMessage\" text=\"" + MessageText + "\" expireTime=\"" + ExpireTime + "\" messageType=\"XMLInterface\"></RK7CMD></RK7Query>";

                                // отправляем запрос
                                var rk = new RKNet_Model.Rk7XML.RK7();
                                var rkResult = rk.SendRequest(ip, xml_request, port, user, password);

                                if(rkResult.Ok)
                                {
                                    var xml_response = rkResult.Data;
                                    _log.LogInformation(".......................................................ответ кассы: " + xml_response);

                                    // дессериализация ответа
                                    try
                                    {
                                        var result = new RKNet_Model.Rk7XML.Response.MainResponse.RK7QueryResult();
                                        var serializer = new XmlSerializer(typeof(RKNet_Model.Rk7XML.Response.MainResponse.RK7QueryResult));
                                        using (TextReader reader = new StringReader(xml_response))
                                        {
                                            result = (RKNet_Model.Rk7XML.Response.MainResponse.RK7QueryResult)serializer.Deserialize(reader);
                                        }

                                        if (result.Status == "Ok")
                                        {
                                            cashState.error = null;
                                            cashState.sended = true;
                                        }
                                        else
                                        {
                                            cashState.error = result.ErrorText;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        cashState.error = e.Message;
                                        _log.LogError("ошибка отправки сообщения на кассу: " + cash.Name + " (" + cash.Ip + "): " + e.ToString());
                                    }
                                }
                                else
                                {
                                    cashState.error = rkResult.ErrorMessage;
                                    _log.LogError("ошибка отправки сообщения на кассу: " + cash.Name + " (" + cash.Ip + "): " + rkResult.ErrorMessage);
                                }

                                                           
                            }
                            // статусы по кассам
                            msg.CashMsgStates = JsonConvert.SerializeObject(cashStates);

                            // статусы сообщения
                            var totalCashes = cashStates.Count();
                            var sendedCashes = cashStates.Where(c => c.sended).Count();

                            if (sendedCashes == totalCashes & msg.FinishedTime == null)
                            {
                                var today = DateTime.Now;
                                msg.FinishedTime = new DateTime(today.Year, today.Month, today.Day, today.Hour, today.Minute, today.Second);
                                msg.State = "доставлено";
                            }

                            if(sendedCashes < totalCashes)
                                msg.State = "доставляется...";                            
                        }

                        mssql.CashMessages.UpdateRange(workMessages);
                        mssql.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e.ToString());
                    }

                }
            }
        }
    }
}
