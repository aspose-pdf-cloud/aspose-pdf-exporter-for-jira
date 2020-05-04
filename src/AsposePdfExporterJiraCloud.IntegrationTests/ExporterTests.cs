using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.IntegrationTests
{
    public class ExporterTests : IClassFixture<IntegrationTestsWebApplicationFactory<Startup>>
    {
        internal IntegrationTestsWebApplicationFactory<Startup> Factory;
        internal readonly ITestOutputHelper Output;
        internal readonly HttpClient Client;
        internal JiraCloudExporterClientServiceMock ClientServiceMock => Factory.AppFixture.ClientServiceMock;
        public ExporterTests(IntegrationTestsWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            Factory = factory;
            Output = output;
            JiraExporterControllerFixture.SetupDownload(ClientServiceMock.PdfApiMock);
            Factory.ClearInvocations();
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async void ExportIssues_Test()
        {
            var request = CallbackTests.CreateRequestWithContent("/app/jiraexporter/export_issues", HttpMethod.Post,
                new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("includeqr", "true"),
                    new KeyValuePair<string, string>("issues", "CCTES-6"),
                    new KeyValuePair<string, string>("issues", "DK-1"),
                })
                );
            var response = await CallbackTests.ExecuteRequest(Client, request, ClientServiceMock.RegistrationData);
            string responseString = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {responseString}");
            dynamic result = JsonConvert.DeserializeObject<dynamic>(responseString);
            Assert.NotNull(result.fileid);
            Assert.Matches("https://mockurl.com/app/JiraExporter/download/[-a-zA-Z0-9]*",
                result.downloadlink.ToString());
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                string id = result.fileid.ToString();
                Assert.Single(context.ReportFile.Where(x => x.UniqueId == id));
            }
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "JiraExporter" && d.ActionName == "ExportIssues"
                && d.Path == "/app/jiraexporter/export_issues" && d.ResultCode == 200
            )));
        }

        [Fact]
        public async void Download_Test()
        {
            // setup fake download file
            var fileId = Guid.NewGuid().ToString();
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                context.ReportFile.Add(new ReportFile
                {
                    UniqueId = fileId,
                    ReportType = "pdf",
                    ContentType = "application/pdf",
                    FileName = "123-Mock-321.pdf",
                    StorageFileName = "mockroot/123-Mock-321.pdf",
                    ClientId = ClientServiceMock.RegistrationData.Id,
                    Created = DateTime.Now,
                    Expired = DateTime.Now.AddHours(1),
                });
                await context.SaveChangesAsync();
            }
            var response = await Client.GetAsync($"/app/jiraexporter/download/{fileId}");
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {Encoding.UTF8.GetString(responseBytes)}");
            Assert.Matches("123-Mock-321.pdf", response.Content.Headers.ContentDisposition.FileNameStar);
            Assert.Equal("file 123-Mock-321.pdf content", Encoding.UTF8.GetString(responseBytes));
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "JiraExporter" && d.ActionName == "GetDownload"
                && d.Path.StartsWith("/app/jiraexporter/download/") && d.ResultCode == 200
            )));
            ClientServiceMock.PdfApiMock.Verify(e => e.DownloadFileAsync("mockroot/123-Mock-321.pdf", It.IsAny<string>(), It.IsAny<string>()));
        }
    }
}
