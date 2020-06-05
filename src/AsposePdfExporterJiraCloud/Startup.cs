using System;
using Aspose.Cloud.Marketplace.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Stubble.Core.Builders;
using Stubble.Extensions.JsonNet;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using Aspose.Cloud.Marketplace.App.Middleware;
using Microsoft.Extensions.Options;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        internal readonly IConfiguration Configuration;
        internal readonly IHostEnvironment Environment;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            CheckConfig(Configuration);

            services.AddEntityFrameworkNpgsql();

            services.AddCors();
            services.AddHttpContextAccessor();
            
            var jiraClientBuilder = services.AddHttpClient("jira_client");
            if (Environment.IsDevelopment())
            {
                // Use dump:true to dump requests/responses. mainly used to populate data for unit testing
                services.AddTransient(f => new LoggingHandler(dump:false));
                jiraClientBuilder.AddHttpMessageHandler<LoggingHandler>();
            }

            // add Configuration expression
            services.AddSingleton<IConfigurationExpression, ConfigurationExpression>();
            // add Url base path replacement service
            services.AddSingleton<IBasePathReplacement>(provider =>
                new BasePathReplacementService(Configuration.GetValue<string>("Settings:BaseAppUrl")));
            
            IConfigurationExpression configurationExpression = new ConfigurationExpression(Configuration);
            services.AddDbContext<DbContext.DatabaseContext>(options =>
                options.UseNpgsql(configurationExpression.Get("ConnectionStrings:DefaultConnection")));

            //Create application service
            services.AddScoped<Services.IAppJiraCloudExporterCli>(provider => {
                var hostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();

                return new Services.JiraCloudExporterClientService(
                    Configuration.GetValue<string>("Settings:AppName"),
                    $"com.aspose.pdf.exporter{Configuration.GetValue<string>("Settings:CustomAppData:CustomTag", "")}",
                    Configuration.GetValue<string>("AsposeCloud:ApiKey"), Configuration.GetValue<string>("AsposeCloud:AppSid"), Configuration.GetValue<string>("AsposeCloud:BasePath", null),
                    hostEnvironment.IsDevelopment());
            });
            // Create ILoggingService to server Elasticsearch logging
            services.AddScoped<ILoggingService>(provider => {
                var configExpression = provider.GetRequiredService<IConfigurationExpression>();
                var hostEnvironment = provider.GetRequiredService<IWebHostEnvironment>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                
                return new ElasticsearchLoggingService(loggerFactory.CreateLogger<ElasticsearchLoggingService>()
                    , Configuration.GetSection("Elasticsearch:Uris")?.Get<string[]>()
                    , configExpression.Get("Elasticsearch:ErrorlogIndex", "errorlog-{DateTime.Now.ToString(\"yyyy.MM.dd\")")
                    , configExpression.Get("Elasticsearch:AccesslogIndex", "accesslog-{DateTime.Now.ToString(\"yyyy.MM.dd\")")
                    , setuplogIndexName: configExpression.Get("Elasticsearch:SetuplogIndex", "setuplog-{DateTime.Now.ToString(\"yyyy.MM.dd\")")
                    , apiId: configExpression.Get("Elasticsearch:apiId"), apiKey: configExpression.Get("Elasticsearch:apiKey")
                    , username: configExpression.Get("Elasticsearch:Username"), password: configExpression.Get("Elasticsearch:Password")
                    , timeoutSeconds: 5
                    , debug: hostEnvironment.IsDevelopment());
            });


            services.AddScheduler(builder =>
            {
                builder.AddJob<Job.RemoveReportJob>();
            });

            services.AddRazorPages()
                .AddRazorPagesOptions(o =>
                {
                    o.Conventions.AddPageRoute("/ExporterContentPane", "app/ExporterContentPane");
                })
                .AddRazorRuntimeCompilation();

            services.AddHealthChecks().AddNpgSql(npgsqlConnectionString: Configuration.GetConnectionString("DefaultConnection"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseMiddleware<Middleware.RequestResponseLoggingMiddleware>();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
                logger.LogInformation("Migrating database...");

                if (Environment.IsDevelopment())
                {
                    //scope.ServiceProvider.GetService<DbContext.DatabaseContext>().Database.EnsureDeleted(); 
                    //scope.ServiceProvider.GetService<DbContext.DatabaseContext>().Database.EnsureCreated();
                }
                // https://stackoverflow.com/questions/50484444/system-invalidoperationexception-relational-specific-methods-can-only-be-used-w
                using (var context = scope.ServiceProvider.GetService<DbContext.DatabaseContext>())
                {
                    if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
                        context.Database.Migrate();
                }

                logger.LogInformation("Migration complete.");
            }

            app.UseCors(builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
            );

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Runs matching. An endpoint is selected and set on the HttpContext if a match is found.
            app.UseRouting();

            //app.UseHttpsRedirection();

            // Initialization order of those middleware matters!
            if (Configuration.GetValue("Settings:UseAccessLogMiddleware", true))
                app.UseMiddleware<ElasticsearchAccessLogMiddleware<Services.IAppJiraCloudExporterCli>>();

            if (Configuration.GetValue("Settings:UseExceptionMiddleware", true))
                app.UseMiddleware<StoreExceptionHandlingMiddleware<Services.IAppJiraCloudExporterCli>>();


            // Executes the endpoint that was selected by routing.
            app.UseEndpoints(endpoints =>
            {
                // Mapping of endpoints goes here:
                endpoints.MapControllers();

                endpoints.MapRazorPages();

                endpoints.MapGet("atlassian-connect.json", async context =>
                {
                    var renderer = new StubbleBuilder()
                        .Configure(settings => {
                            settings.SetIgnoreCaseOnKeyLookup(true);
                            settings.SetMaxRecursionDepth(512);
                            settings.AddJsonNet();
                        })
                      .Build();
                    var appData = Configuration.GetSection("Settings:CustomAppData").GetChildren().ToDictionary(t => t.Key, t => t.Value);
                    appData["BaseAppUrl"] = Configuration.GetValue<string>("Settings:BaseAppUrl");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        JsonConvert.SerializeObject(JsonConvert.DeserializeObject(
                        renderer.Render(File.ReadAllText(Configuration.GetValue("Templates:AppDescriptorTemplate", "template/app-descriptor-template.Mustache")), appData)
                        ))
                    );
                });
                endpoints.MapGet("config", async context =>
                {
                    await context.Response.WriteAsync(
                        JsonConvert.SerializeObject(new
                        {
                            connect= $"{Configuration.GetValue<string>("Settings:BaseAppUrl")}/atlassian-connect.json",
                            version = $"{Assembly.GetExecutingAssembly().GetName().Version}"
                        }, Formatting.Indented)
                    );
                });

                endpoints.MapFallbackToFile("index.html");
            });

            app.UseHealthChecks("/status", new HealthCheckOptions()
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    string result = "Healthy";
                    switch(report.Status)
                    {
                        case Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy: result = "Failure";break;
                        case Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded: result = "Degraded"; break;
                    }
                    await context.Response.WriteAsync(result);
                }
            });
        }

        internal void CheckConfig(IConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")))
                throw new ArgumentException("Connection is not specified", "ConnectionStrings:DefaultConnection");
            if (string.IsNullOrWhiteSpace(config.GetValue<string>("Settings:AppName")))
                throw new ArgumentException("Application name is not specified", "Settings:AppName");
            if (string.IsNullOrWhiteSpace(config.GetValue<string>("AsposeCloud:ApiKey"))||
                string.IsNullOrWhiteSpace(config.GetValue<string>("AsposeCloud:AppSid")))
                throw new ArgumentException("Aspose.Cloud's AppSid/AppKey were not specified. You can obtain them at https://dashboard.aspose.cloud", "AppSid/AppKey");
        }
    }
}
