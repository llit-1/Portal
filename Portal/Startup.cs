using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portal.Models;
using System;

namespace Portal
{
    public class Startup
    {
        public bool IsTest { get; set; }
        // Запуск приложения
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            SettingsInternal.Configuration = configuration;

            // логируем запуск и обновление версии портала
            var log = new LogEvent<string>();

            if (log.isNewVersion())
            {
                log.Name = "Портал обновлен";
                log.Save();
            }
            log.Name = "Портал запущен";
            log.Save();
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IsTest = Configuration.GetSection("TestMode")["test"] != "0";
            var SessionMinutes = 60; // время в минутах
            // CookieAuthenticationOptions
            //services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromSeconds(10));
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Account/Login");
                    options.LogoutPath = new Microsoft.AspNetCore.Http.PathString("/Account/Logout");
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(SessionMinutes); // время истечения срока действия куки
                    options.SlidingExpiration = true;
                });

            services.AddControllersWithViews()
                .AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            services.AddRazorPages().AddRazorRuntimeCompilation();
            if (IsTest)
            { 
                services.AddDbContext<DB.MSSQLDBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("mssqltest")), ServiceLifetime.Transient);
                services.AddDbContext<DB.CalculatorDBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("calculatortest")), ServiceLifetime.Transient);
            }
            else
            {
                services.AddDbContext<DB.MSSQLDBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("mssql")), ServiceLifetime.Transient);
                services.AddDbContext<DB.CalculatorDBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("calculator")), ServiceLifetime.Transient);
            }
            services.AddDbContext<DB.Reports1CDBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("mssql1C")), ServiceLifetime.Transient);
            services.AddDbContext<DB.RK7DBContext>(options => options.UseSqlServer(Configuration.GetConnectionString("mssqlrk7")), ServiceLifetime.Transient);
            services.AddDbContext<DB.SQLiteDBContext>(options => options.UseSqlite(Configuration.GetConnectionString("sqlite")));

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = ".PortalLL.Session";
                options.IdleTimeout = TimeSpan.FromMinutes(SessionMinutes);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = false;
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddHostedService<HostedServices.AIShocaseService>();
           // services.AddHostedService<HostedServices.PhotoCamService>();
            services.AddHostedService<HostedServices.CashMessagesService>();
            services.AddHostedService<HostedServices.SkuStopService>();

            services.AddResponseCaching();
            services.AddHttpClient();
            // Выдача потокового видео
            services.AddScoped<Portal.Services.IStreamVideoService, Portal.Services.StreamVideoService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStopping.Register(OnShutdown);

            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //    ApiRequest.Host = "https://localhost:44340";
            //}
            //else
            //{
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            if (IsTest)
            {
                ApiRequest.Host = Configuration.GetSection("Host")["test"];
            }
            else
            {
                ApiRequest.Host = Configuration.GetSection("Host")["default"];
            }

            //}
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseResponseCaching();

            app.UseRouting();

            app.UseAuthentication();    // аутентификация
            app.UseAuthorization();     // авторизация            

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "Audits",
                  pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
            });

        }

        // Остановка приложения
        private void OnShutdown()
        {
            var log = new LogEvent<string>();
            log.Name = "Портал остановлен";
            log.Save();
        }
    }
}
