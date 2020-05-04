using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Dynamic.Core;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect.Tests;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Controllers;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.Services;
using Aspose.Pdf.Cloud.Sdk.Api;
using Aspose.Pdf.Cloud.Sdk.Model;
using AutoFixture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class JiraExporterControllerFixture : ControllerFixture<JiraExporterController>
    {
        public Mock<IHttpClientFactory> HttpFactoryMock;
        public Mock<HttpMessageHandler> HttpMessageHandlerMock;
        public HttpClient HttpClientMock;

        public override void Initialize()
        {
            base.Initialize();
            HttpFactoryMock = new Mock<IHttpClientFactory>();
            (HttpMessageHandlerMock, HttpClientMock) = RestApiClientTests.GetJiraHttpClientMock(
                "responses"
                , new Uri("https://mockjiracloud.com"));
            HttpFactoryMock.Setup(e => e.CreateClient("jira_client")).Returns(HttpClientMock);
            Configuration = new Dictionary<string, string>()
            {
                {"Settings:StorageRoot", "mockroot"},
            };

            ClientServiceMock.PdfApiMock = SetupDownload(ClientServiceMock.PdfApiMock);
        }

        public override void ClearInvocations()
        {
            base.ClearInvocations();
            HttpFactoryMock.Invocations.Clear();
            HttpMessageHandlerMock.Invocations.Clear();
        }

        public override IServiceCollection ProvideServices(IServiceCollection c) => 
            base.ProvideServices(c).AddScoped(p => HttpFactoryMock.Object);


        public static Mock<IPdfApi> SetupDownload(Mock<IPdfApi> PdfApiMock)
        {
            var fixture = new Fixture();
            PdfApiMock.Setup(f => f.DownloadFileAsync("mockroot/123-Mock-321.pdf", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(async () => await Task.FromResult((Stream)new MemoryStream(Encoding.UTF8.GetBytes("file 123-Mock-321.pdf content"))));
            PdfApiMock.Setup(f => f.DownloadFileAsync("mockroot/123-ErrorMock-321.json", It.IsAny<string>(), It.IsAny<string>()))
                .Returns(async () => await Task.FromResult((Stream)new MemoryStream(Encoding.UTF8.GetBytes("file 123-ErrorMock-321.json content"))));

            PdfApiMock.Setup(f => f.GetFileVersionsAsync("mockroot/123-Mock-321.pdf", It.IsAny<string>()))
                .Returns(() => Task.FromResult(fixture.Create<FileVersions>()));

            return PdfApiMock;
        }
    }


    public class JiraExporterControllerTests : IClassFixture<JiraExporterControllerFixture>
    {
        internal JiraExporterControllerFixture Fixture;
        internal ITestOutputHelper Output;
        internal JiraExporterController Controller => Fixture.Controller;
        internal JiraCloudExporterClientServiceMock ClientServiceMock => Fixture.ClientServiceMock;
        internal Mock<ILoggingService> LoggingServiceMock => Fixture.LoggingServiceMock;
        public JiraExporterControllerTests(JiraExporterControllerFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
            Fixture.ClearInvocations();
        }

        public static ControllerContext CreateControllerContext(Dictionary<string, string> routes, byte[] requestData = null)
        {
            var request = new Mock<HttpRequest>();
            request.SetupGet(e => e.Body).Returns(new MemoryStream(requestData ?? new byte[]{}));
            var context = new Mock<HttpContext>();
            context.SetupGet(x => x.Request).Returns(request.Object);

            return new ControllerContext(new ActionContext
            {
                HttpContext = context.Object,
                RouteData = new RouteData(new RouteValueDictionary(routes)),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            });
        }

        [Fact]
        public async Task ExportIssues_Test()
        {
            var actionResult = await Controller.ExportIssues(new List<string> { "CCTES-6", "DK-1" } // "DK-1" is an epic
                , true, "", "", "", "");
            Assert.True(actionResult is OkObjectResult, $"actionResult should be OkObjectResult, not {actionResult.GetType().Name}");
            var objectResult = actionResult as OkObjectResult;
            Assert.NotNull(objectResult);

            var resultStr = JsonConvert.SerializeObject(objectResult.Value);
            var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultStr);
            Assert.Matches("http://mockaddr/mockdownload.*", result["downloadlink"]);
            Assert.Matches("[-a-zA-Z0-9]*", result["fileid"]);
            Fixture.HttpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(m => 
                m.RequestUri.ToString() == "https://mockjiracloud.com/rest/api/3/issue/DK-1"), ItExpr.IsAny<CancellationToken>());
            Fixture.HttpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(m =>
                m.RequestUri.ToString() == "https://mockjiracloud.com/rest/api/3/issue/CCTES-6"), ItExpr.IsAny<CancellationToken>());
            Fixture.HttpMessageHandlerMock.Protected().Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(m =>
                m.RequestUri.ToString() == "https://mockjiracloud.com/rest/api/3/search?jql=\"Epic Link\" = \"DK-1\""), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task Download_Test()
        {
            string id = Guid.NewGuid().ToString();
            await using (var context = Fixture.NewDatabaseContext())
            {
                ClientRegistration reg = context.ClientRegistration.First();
                context.ReportFile.Add(new ReportFile
                {
                    UniqueId = id,
                    ClientId = reg.Id,
                    StorageFileName = "mockroot/123-Mock-321.pdf",
                    FileName = "MockFile.pdf",
                    ContentType = "application/pdf"
                });
                await context.SaveChangesAsync();
            }
            var actionResult = await Controller.GetDownload(id, "", "", "", "");
            Assert.True(actionResult is FileStreamResult, $"r should be FileStreamResult, not {actionResult.GetType().Name}");
            var fileStreamResult = actionResult as FileStreamResult;

            Assert.NotNull(fileStreamResult);
            Assert.Equal("application/pdf", fileStreamResult.ContentType);
            Assert.Equal("MockFile.pdf", fileStreamResult.FileDownloadName);
            await using var ms = new MemoryStream();
            await fileStreamResult.FileStream.CopyToAsync(ms);
            Assert.Equal("file 123-Mock-321.pdf content", Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
}
