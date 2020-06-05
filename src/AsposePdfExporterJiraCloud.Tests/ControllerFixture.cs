using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.DbContext;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using Aspose.Cloud.Marketplace.Report;
using Aspose.Cloud.Marketplace.Services;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using Aspose.Pdf.Cloud.Sdk.Api;
using Aspose.BarCode.Cloud.Sdk.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class AppEnvironmentFixture
    {
        public JiraCloudExporterClientServiceMock ClientServiceMock;
        public Dictionary<string, string> Configuration;
        public Mock<IUrlHelper> UrlHelperMock;
        public string DbName;
        public Mock<ILoggingService> LoggingServiceMock;
        public Mock<IBasePathReplacement> PathReplacementMock;

        public AppEnvironmentFixture()
        {
            DbName = Guid.NewGuid().ToString();
            Configuration = new Dictionary<string, string>();
            
            Initialize();
            ClearInvocations();
        }

        /// <summary>
        /// In this method we are initializing DbContextOptionsBuilder
        /// For our tests we are using InMemoryDatabase
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder DbContextOptionsBuilder(DbContextOptionsBuilder options, string dbName) =>
            options.UseInMemoryDatabase(dbName);

        public static DbContextOptionsBuilder<DatabaseContext> DbContextOptionsBuilder(
            DbContextOptionsBuilder<DatabaseContext> options, string dbName) =>
            (DbContextOptionsBuilder<DatabaseContext>)DbContextOptionsBuilder((DbContextOptionsBuilder)options, dbName);

        public static DatabaseContext NewDatabaseContext(string dbName) => new DatabaseContext(
            DbContextOptionsBuilder(options: new DbContextOptionsBuilder<DatabaseContext>(), dbName: dbName).Options
            , null, null);

        public DatabaseContext NewDatabaseContext() => NewDatabaseContext(dbName:DbName);

        public virtual void ClearInvocations()
        {
            ClientServiceMock.ClearInvocations();
            LoggingServiceMock.Invocations.Clear();
            PathReplacementMock.Invocations.Clear();
            UrlHelperMock.Invocations.Clear();
        }

        public virtual void Initialize()
        {
            PathReplacementMock = new Mock<IBasePathReplacement>();
            PathReplacementMock.Setup(e => e.ReplaceBaseUrl(It.IsAny<string>()))
                .Returns((string url) => url.Replace("localaddr", "mockaddr"));
            PathReplacementMock.Setup(e => e.ReplaceBaseUrl(It.IsAny<Uri>()))
                .Returns((Uri url) => new Uri(url.ToString().Replace("localaddr", "mockaddr")));

            UrlHelperMock = new Mock<IUrlHelper>();
            UrlHelperMock.Setup(x => x.Link("GetDownload", It.IsAny<object>()))
                .Returns((string routeName, object values) => $"http://localaddr/mockdownload/{values?.GetType().GetProperty("id")?.GetValue(values, null)}");
            UrlHelperMock.Setup(x => x.Action(It.Is<UrlActionContext>( m => m.Controller == "JiraExporter" && m.Action == "ExportIssues")))
                .Returns("/mock/export/action");
            LoggingServiceMock = new Mock<ILoggingService>();
            LoggingServiceMock.Setup(e => e.ReportSetupLog(It.IsAny<ElasticsearchSetupDocument>())).Returns(Task.CompletedTask);
            LoggingServiceMock.Setup(e => e.ReportAccessLog(It.IsAny<ElasticsearchAccessLogDocument>())).Returns(Task.CompletedTask);

            ClientServiceMock = new JiraCloudExporterClientServiceMock(PdfExporter.Tests.PdfReport_Tests.Setup(PdfExporter.Tests.PdfReportPageProcessorFixture.Setup(new Mock<IPdfApi>()))
                , PdfExporter.Tests.PdfReportPageProcessorFixture.Setup(new Mock<IBarcodeApi>()));

            // dummy database initialization
            (DbName, ClientServiceMock.RegistrationData) = DatabaseInitialization.InitializeNewDatabase();
        }

        public virtual IServiceCollection ProvideServices(IServiceCollection c) =>
            c.AddLogging(c => { c.AddDebug(); })
                .AddDbContext<DatabaseContext>(options => DbContextOptionsBuilder(options, DbName))
                .AddScoped<IAppJiraCloudExporterCli>(provider => ClientServiceMock)
                .AddScoped(provider => LoggingServiceMock.Object)
                .AddSingleton<IConfiguration>(provider => new ConfigurationBuilder().AddInMemoryCollection(Configuration).Build())
                .AddSingleton(PathReplacementMock.Object)
                .AddSingleton<IConfigurationExpression, ConfigurationExpression>()
                .AddSingleton(provider =>
                {
                    var moqHostEnvironment = new Mock<IWebHostEnvironment>();
                    moqHostEnvironment.Setup(h => h.EnvironmentName).Returns("Development");
                    return moqHostEnvironment.Object;
                });

    }
    /// <summary>
    /// Base controller fixture passed by XUnit framework into each controller test
    /// </summary>
    /// <typeparam name="T">Controller class</typeparam>
    public class ControllerFixture<T> : AppEnvironmentFixture where T : class 
    {
        public T Controller;
        public ControllerFixture()
        {
            Controller = ActivatorUtilities.CreateInstance<T>(ProvideServices(new ServiceCollection()).BuildServiceProvider());
            (Controller as ControllerBase).Url = UrlHelperMock.Object;
        }
    }
}
