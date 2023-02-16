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
    public class AIShocaseService : IHostedService
    {
        //--------------------------------------------------------------------------------------
        // Служба анализа заполненности полок по расписанию 
        //--------------------------------------------------------------------------------------

        bool Enabled = false;
        protected static int updateTime = 5; // периодичность проверки изменений в расписании зон в минутах
        
        protected IServiceProvider serviceProvider;
        protected List<ZoneTask> ZoneTasks;
        Timer serviceTimer;
        private readonly ILogger _log;

        // Конструктор
        public AIShocaseService(IServiceProvider services, ILogger<AIShocaseService> log)
        {
            serviceProvider = services;
            ZoneTasks = new List<ZoneTask>();
            _log = log;          
        }

        // Запуск службы
        public Task StartAsync(CancellationToken cancellationToken)
        {
            serviceTimer = new Timer(GetZones, null, 0, 60000 * updateTime);


            using (var scope = serviceProvider.CreateScope())
            {
                try
                {
                    var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                    Enabled = sqlite.Modules.FirstOrDefault(m => m.Name == "AIShowcase").Enabled;
                }
                catch (Exception e)
                {
                    
                }
            }


            if (Enabled)
            {                 
                _log.LogInformation("AIShowcase Service started");
            } 
            else
            {
                serviceTimer.Change(Timeout.Infinite, 0);
                _log.LogInformation("AIShowcase Service disabled");
            }

            return Task.CompletedTask;
        }

        // Остановка службы
        public Task StopAsync(CancellationToken cancellationToken)
        {
            serviceTimer.Change(Timeout.Infinite, 0);
            _log.LogInformation("AIShowcase Service stopped");
            return Task.CompletedTask;
        }

        //--------------------------------------------------------------------------------------
        // Методы 
        //--------------------------------------------------------------------------------------

        void GetZones(object state)
        {
            if(Enabled)
            {
                _log.LogInformation("AIShowcase refreshing zones...");

                using (var scope = serviceProvider.CreateScope())
                {
                    try
                    {
                        var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                        var dbZones = sqlite.zones.ToList();
                        var newZones = new List<Zone>();

                        // Удаляем изменившиеся или удаленные зоны из списка активных заданий
                        foreach (var zt in ZoneTasks)
                        {
                            bool needRemove = true;
                            foreach (var nz in dbZones)
                            {
                                if (ZoneSame(zt.zone, nz))
                                {
                                    needRemove = false;
                                }
                            }
                            if (needRemove)
                            {
                                zt.Stop();
                                ZoneTasks.Remove(zt);
                            }
                        }

                        // Готовим список зон, которых еще нет в списке активных заданий
                        foreach (var z in dbZones)
                        {
                            var needAdd = true;
                            foreach (var zt in ZoneTasks)
                            {
                                if (ZoneSame(z, zt.zone))
                                {
                                    needAdd = false;
                                }
                            }
                            if (needAdd) newZones.Add(z);
                        }

                        // Добавляем в список активных заданий новые зоны
                        if (newZones.Count() > 0)
                        {
                            foreach (var z in newZones)
                            {
                                var newTask = new ZoneTask(z, serviceProvider);
                                ZoneTasks.Add(newTask);
                                newTask.Start();
                            }
                        }

                    }
                    catch (Exception e)
                    {

                    }

                }
            }            
        }

        // Сравнение двух зон
        bool ZoneSame(Zone a, Zone b)
        {
            bool result;

            if
            (
                a.Id == b.Id &
                a.Interval == b.Interval &
                a.Level == b.Level &
                a.Name == b.Name &
                a.PolyPoints == b.PolyPoints &
                a.SourceImage == b.SourceImage &
                a.StartTime == b.StartTime &
                a.StopTime == b.StopTime &
                a.VmsType == b.VmsType
             ) result = true;
            else result = false;

            return result;
        }
    }

    //--------------------------------------------------------------------------------------
    // Класс сбора данных заполненности витрин. Для каждой зоны создается свой экземпляр. 
    //--------------------------------------------------------------------------------------
    public class ZoneTask
    {        
        internal Zone zone;
        IServiceProvider servProv;
        internal Timer timer;        

        // Конструктор
        public ZoneTask(Zone Zone, IServiceProvider serviceProvider)
        {
            zone = Zone;
            servProv = serviceProvider;                    
        }

        // Сбор и запись данных
        internal void DataCollect(object state)
        {
            using (var scope = servProv.CreateScope())
            {
                try
                {
                    var start_time = TimeSpan.ParseExact(zone.StartTime, "g" , System.Globalization.CultureInfo.InvariantCulture);
                    var end_time = TimeSpan.ParseExact(zone.StopTime, "g", System.Globalization.CultureInfo.InvariantCulture);
                    var now_time = DateTime.Now.TimeOfDay;
                    
                    // Если текущее время попадает в диапазон для анализа
                    if (now_time >= start_time & now_time <= end_time)
                    {
                        var sqlite = (DB.SQLiteDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.SQLiteDBContext));
                        var mssql = (DB.MSSQLDBContext)scope.ServiceProvider.GetRequiredService(typeof(DB.MSSQLDBContext));

                        var data = new Models.MSSQL.AIShocase();
                        var cam = sqlite.NxCameras.Include(c => c.TT).Include(c => c.NxSystem).First(c => c.Guid == zone.CameraGuid);

                        // Получаем текущее изображение с камеры
                        var nx = new module_NX.NX();
                        var camPicture = nx.GetCameraPicture(DateTime.Now, cam, 700);
                        byte[] secondImage;

                        using (var ms = new System.IO.MemoryStream())
                        {
                            camPicture.Data.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                            secondImage = ms.ToArray();
                        }

                        // Отправляем два изображения на сравнения в модуль
                        //var AIShocaseModule = new module_AIShowcase.Compare2Image(cam.SourceImage, secondImage, zone.PolyPoints, int.Parse(zone.Level));


                        // Записываем результаты в БД
                        data.ZoneId = zone.Id;
                        data.TT = cam.TT.Name;
                        data.ZoneName = zone.Name;
                        data.DateTime = DateTime.Now.ToString("yyyy.MM.dd HH:mm");
                        //data.Value = AIShocaseModule.Value;
                        //data.SourceImage = cam.SourceImage;
                        data.SecondImage = secondImage;


                        mssql.AIShocases.Add(data);
                        mssql.SaveChanges();
                    }
                }
                catch(Exception e)
                {
                    //System.Diagnostics.Debug.WriteLine(e.Message);
                    //System.Diagnostics.Debug.WriteLine(e.ToString());
                }
            }
                

        }
        // Запуск
        public void Start()
        {
            timer = new Timer(DataCollect, null, 0, 60000 * int.Parse(zone.Interval));
        }

        // Остановка
        public void Stop()
        {
            timer.Dispose();
        }
    }   
}
