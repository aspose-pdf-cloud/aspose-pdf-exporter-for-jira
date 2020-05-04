using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests;
using Aspose.Cloud.Marketplace.Services;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net.Http;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect.Tests;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.IntegrationTests
{
    /// <summary>
    /// Custom WebApplicationFactory to produce mocks for application created by TestServer
    ///
    /// https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1#customize-webapplicationfactory
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>

    public class IntegrationTestsWebApplicationFactory<TStartup>
        : WebApplicationFactory<TStartup> where TStartup : class
    {
        public AppEnvironmentFixture AppFixture;
        public Dictionary<string, string> ConfigMock;
        public Mock<ILoggingService> LoggingMock;
        public Mock<IHttpClientFactory> HttpFactoryMock;
        public Mock<HttpMessageHandler> HttpMessageHandlerMock;
        public HttpClient HttpClientMock;
        
        public IntegrationTestsWebApplicationFactory()
        {
            AppFixture = new AppEnvironmentFixture();
            // setup ILoggingService mocks
            LoggingMock = new Mock<ILoggingService>();

            LoggingMock.Setup(e => e.ReportAccessLog(It.IsAny<ElasticsearchAccessLogDocument>())).Returns(Task.CompletedTask);
            LoggingMock.Setup(e => e.ReportErrorLog(It.IsAny<ElasticsearchErrorDocument>())).Returns(Task.CompletedTask);
            LoggingMock.Setup(e => e.ReportSetupLog(It.IsAny<ElasticsearchSetupDocument>())).Returns(Task.CompletedTask);

            ConfigMock = new Dictionary<string, string>()
            {
                {"Settings:StorageRoot", "mockroot"},
                {"Settings:BaseAppUrl", "https://mockurl.com"},
            };

            HttpFactoryMock = new Mock<IHttpClientFactory>();
            (HttpMessageHandlerMock, HttpClientMock) = RestApiClientTests.GetJiraHttpClientMock(
                "responses"
                , new Uri("https://mockjiracloud.com"));
            HttpFactoryMock.Setup(e => e.CreateClient("jira_client")).Returns(HttpClientMock);
        }

        public void ClearInvocations()
        {
            AppFixture.ClearInvocations();
            LoggingMock.Invocations.Clear();
            HttpFactoryMock.Invocations.Clear();
            HttpMessageHandlerMock.Invocations.Clear();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .UseSolutionRelativeContentRoot("AsposePdfExporterJiraCloud")
                .ConfigureAppConfiguration((context, conf) =>
                {
                    conf.AddInMemoryCollection(ConfigMock.ToList());
                })
                .ConfigureServices(services =>
                {
                    // Replace IAppGithubExporterCli with mocked version
                    services.Replace(ServiceDescriptor.Scoped<IAppJiraCloudExporterCli>(p => AppFixture.ClientServiceMock));

                    // Replace ILoggingService with mocked version
                    services.Replace(ServiceDescriptor.Scoped(p => LoggingMock.Object));

                    // Replace IHttpClientFactory with mocked version
                    services.Replace(ServiceDescriptor.Scoped(p => HttpFactoryMock.Object));
                    // Replace DatabaseContext with mocked version
                    services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DbContext.DatabaseContext>)));
                    services.Remove(services.SingleOrDefault(d => d.ServiceType == typeof(DbContext.DatabaseContext)));
                    services.AddDbContext<DbContext.DatabaseContext>(options => 
                        AppEnvironmentFixture.DbContextOptionsBuilder(options, AppFixture.DbName)
                            .ConfigureWarnings(o => o.Ignore())
                            );
                });

        }
    }
}
