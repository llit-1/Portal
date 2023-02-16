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

namespace Portal.HostedServices
{
    public class PhotoCamService : IHostedService
    {
        //--------------------------------------------------------------------------------------
        // Служба сбора фотографий с камер видеонаблюдения по расписанию 
        //--------------------------------------------------------------------------------------

        Portal.Models.Settings.Module module;

        protected IServiceProvider serviceProvider;
        Timer serviceTimer;
        private readonly ILogger _log;

        // Конструктор
        public PhotoCamService(IServiceProvider services, ILogger<PhotoCamService> log)
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
                    module = sqlite.Modules.FirstOrDefault(m => m.Name == "PhotoCam");
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

                        var module = sqlite.Modules.FirstOrDefault(m => m.Name == "PhotoCam");

                        var start_time = TimeSpan.ParseExact(module.StartTime, "g", System.Globalization.CultureInfo.InvariantCulture);
                        var end_time = TimeSpan.ParseExact(module.StopTime, "g", System.Globalization.CultureInfo.InvariantCulture);
                        var interval =  new TimeSpan(0, int.Parse(module.Interval), 0);
                        var now_time = DateTime.Now.TimeOfDay;

                        // определяем и перебираем все времена для съёмки кратно заданному в настройках интервалу
                        for (var time = start_time; time <= end_time; time += interval)
                        {
                            // если интервал уже наступил
                            if (now_time >= time)                           
                            {
                                _log.LogInformation("получение снимков с камер за время: " + time);

                                DateTime datetime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, time.Hours, time.Minutes, 0);                               
                                var cameras = sqlite.NxCameras
                                    .Include(c => c.TT)
                                    .Include(c => c.NxSystem)
                                    .Include(c => c.CamGroup)
                                    .Where(c => c.TT != null)
                                    .Where(c => c.CamGroup.Id == null || c.CamGroup.Id == 1)
                                    .ToList();

                                // Получаем изображения с камер
                                foreach (var tt in cameras.GroupBy(t => t.TT.Name).OrderBy(n => n.Key))
                                {
                                    string error = "";
                                    foreach (var cam in tt)
                                    {
                                        // проверяем на наличие снимка данной камеры за данное времени данной даты в БД
                                        var exist = mssql.PhotoCams.Where(p => p.dateTime == datetime & p.camId == cam.Id).Count();

                                        if (exist > 0)
                                        {
                                            //_log.LogInformation("снимок с текущей камеры за текущее время уже есть в базе данных");
                                        }
                                        else
                                        {
                                            var data = new Models.MSSQL.PhotoCam();
                                            var nx = new module_NX.NX();
                                            var camPicture = nx.GetCameraPicture(datetime, cam, 700);
                                            byte[] byteImage;

                                            if (camPicture.Ok)
                                            {
                                                using (var ms = new System.IO.MemoryStream())
                                                {
                                                    camPicture.Data.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                                                    byteImage = ms.ToArray();
                                                }

                                                // Записываем результаты в БД
                                                data.TTCode = cam.TT.Code;
                                                data.TTName = cam.TT.Name;
                                                data.dateTime = datetime;
                                                data.camId = cam.Id;
                                                data.camName = cam.Name;
                                                data.Image = byteImage;
                                                if (cam.CamGroup != null)
                                                {
                                                    data.groupId = cam.CamGroup.Id;
                                                    data.groupName = cam.CamGroup.Name;
                                                }

                                                mssql.PhotoCams.Add(data);
                                            }
                                            else
                                            {
                                                error = camPicture.ErrorMessage;
                                            }
                                        }
                                    }
                                    mssql.SaveChanges();
                                    if (error == "")
                                    {
                                        _log.LogInformation(tt.Key + "...............Ok");
                                    }
                                    else
                                    {
                                        _log.LogWarning(tt.Key + "..............." + error);
                                    }
                                }
                                
                                var next = time + interval;
                                if (next > end_time) next = start_time;
                                _log.LogInformation("выполнено, следующий интервал: " + next);
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
    }
}
