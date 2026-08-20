using System;
using System.IO;
using DevExpress.AspNetCore;
using DevExpress.AspNetCore.Reporting;
using DevExpress.Security.Resources;
using DevExpress.XtraReports.Web.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HotelSys.Services;
using HotelSys.Data;
using HotelSys.Integrations.Appwrite;
using LinqToDB.AspNet;
using DataModels;
using LinqToDB.Configuration;
using LinqToDB.AspNet.Logging;
using Microsoft.AspNetCore.Http.Features;
using DevExpress.XtraReports.Web.WebDocumentViewer;

namespace HotelSys {
    public class Startup {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddDevExpressControls();
            // services.AddScoped<ReportStorageWebExtension, CustomReportStorageWebExtension>();
            services.AddTransient<IWebDocumentViewerReportResolver, CustomWebDocumentViewerReportResolver>();


            services
               .AddMvc().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null)
               .AddNewtonsoftJson()
               .SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_3_0);
            services.ConfigureReportingServices(configurator => {
                configurator.ConfigureReportDesigner(designerConfigurator => {
                    designerConfigurator.RegisterDataSourceWizardConfigFileConnectionStringsProvider();
                    designerConfigurator.RegisterObjectDataSourceWizardTypeProvider<ObjectDataSourceWizardCustomTypeProvider>();
                    designerConfigurator.RegisterObjectDataSourceConstructorFilterService<CustomObjectDataSourceConstructorFilterService>();
                });
                configurator.ConfigureWebDocumentViewer(viewerConfigurator => {
                    viewerConfigurator.UseCachedReportSourceBuilder();
                });
            });


            //services.AddDbContext<ReportDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("ReportsDataConnectionString")));


            services.Configure<AppwriteSyncOptions>(Configuration.GetSection("Appwrite"));
            services.AddHttpClient<AppwriteRestClient>();
            services.AddScoped<AppwriteRoomSyncService>();
            services.AddScoped<AppwriteBookingSyncService>();
            services.AddScoped<AppwriteGuestInfoSyncService>();
            services.AddScoped<AppwritePaymentSyncService>();
            services.AddHostedService<AppwriteRoomSyncHostedService>();
            services.AddHostedService<AppwriteCoreSyncHostedService>();

            services.AddControllersWithViews();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/auth/login";
                    options.AccessDeniedPath = "/auth/login";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });
            services.AddAuthorization();

            services.AddLinqToDBContext<HotelAlkheerDB>((provider, options) =>
            {
                options
                    .UseSqlServer(Configuration.GetConnectionString("cc"))
                    .UseDefaultLogging(provider);
            });

            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue; //not recommended value
                options.MultipartBodyLengthLimit = long.MaxValue; //not recommended value
            });


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, HotelAlkheerDB db) {
            //db.InitializeDatabase();
            //db = new HotelAlkheerDB();
            var contentDirectoryAllowRule = DirectoryAccessRule.Allow(new DirectoryInfo(Path.Combine(env.ContentRootPath, "..", "Content")).FullName);
            AccessSettings.ReportingSpecificResources.TrySetRules(contentDirectoryAllowRule, UrlAccessRule.Allow());


            var reportingLogger = loggerFactory.CreateLogger("DXReporting");

            var reportingLogger1 = loggerFactory.CreateLogger("HotelAlkheerDB");

            DevExpress.XtraReports.Web.ClientControls.LoggerService.Initialize((exception, message) => {
                var logMessage = $"[{DateTime.Now}]: Exception occurred. Message: '{message}'. Exception Details:\r\n{exception}";
                reportingLogger.LogError(logMessage);
            });
            DevExpress.XtraReports.Configuration.Settings.Default.UserDesignerOptions.DataBindingMode = DevExpress.XtraReports.UI.DataBindingMode.Expressions;
            app.UseDevExpressControls();
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;





           
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Wellcome1}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });

            //app.UseAuthorization();
            //app.UseEndpoints(endpoints => {
            //    endpoints.MapControllerRoute(
            //        name: "default",
            //        pattern: "{controller=Home}/{action=Index}/{id?}");
            //});
        }
    }
}