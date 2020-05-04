using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using AtlModel = Aspose.Cloud.Marketplace.App.Atlassian.Connect.Model;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Net;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using Aspose.Cloud.Marketplace.Common;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using Aspose.Cloud.Marketplace.Report;
using Aspose.Cloud.Marketplace.Services;
using System.Net.Http;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Controllers
{
    /// <summary>
    /// Implements report creation and download functions
    /// </summary>
    [Route("app/[controller]")]
    [ApiController]
    public class JiraExporterController : ControllerBase
    {
        private readonly DbContext.DatabaseContext _dbContext;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<JiraExporterController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAppJiraCloudExporterCli _client;
        private readonly IBasePathReplacement _basePathReplacement;
        private readonly IHttpClientFactory _clientFactory;

        public JiraExporterController(DbContext.DatabaseContext dbContext, ILogger<JiraExporterController> logger, IWebHostEnvironment hostEnvironment
            , IConfiguration configuration, Services.IAppJiraCloudExporterCli client, IBasePathReplacement basePathReplacement, IHttpClientFactory clientFactory)
        {
            _dbContext = dbContext;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _client = client;
            _basePathReplacement = basePathReplacement;
            _clientFactory = clientFactory;
        }


        private string getField(JArray fields, string custom)
        {
            JToken token = fields.FirstOrDefault(t => t.SelectToken("$.schema.custom")?.ToString() == custom);
            return token == null ? null : token["id"]?.ToString();
        }

        private async Task<JArray> getSubtasksForEpic(RestApiClient cli, string issueName)
        {
            string issues = await cli.Get($"/rest/api/3/search", $"jql=\"Epic Link\" = \"{issueName}\"");
            return ((JObject)JsonConvert.DeserializeObject(issues))["issues"] as JArray;
        }


        [HttpPost("export_issues", Name = "ExportIssues")]
        [MiddlewareFilter(typeof(Middleware.JiraAuthPipeline))]
        public async Task<IActionResult> ExportIssues([FromForm(Name = "issues")]List<string> issuesList, [FromForm(Name = "includeqr")]bool includeQRCode
            , [FromHeader(Name = "Referer")]string referer, [FromHeader(Name = "x-real-ip")]string realIp, [FromHeader(Name = "User-Agent")]string userAgent
            , [FromHeader(Name = "x-forwarded-for")]string forwardedFor)
        {
            IActionResult result = null;
            DateTime requestDateTime = DateTime.Now;
            TaskMeasurer taskMeasurer = new TaskMeasurer();
            string extension = "pdf";
            Model.ClientRegistration registrationData = _client.RegistrationData; 
            string fileId = Guid.NewGuid().ToString();
            string reportFileName = $"{_configuration.GetValue<string>("Settings:StorageRoot", "clients_jiracloud")}/{registrationData.ClientKey}/{fileId}.{extension}";

            JArray fields = null;
            JObject issues = null;
            Dictionary<string, JArray> epicStories = new Dictionary<string, JArray>();
            Report.Model.Document document = null;
            Model.ReportFile reportFile = null;
            ReportJiraCloudModel reportJiraCloudModel = null;
            List<StatisticalDocument> statItems = new List<StatisticalDocument>();
            try
            {
                _logger.LogInformation($"{requestDateTime} New job ClientKey {registrationData.ClientKey}, issues {String.Join(", ", issuesList)}");
                HttpClient httpCli = _clientFactory.CreateClient("jira_client");
                httpCli.BaseAddress = new Uri(registrationData.BaseUrl);
                RestApiClient cli = new RestApiClient( registrationData.Key, registrationData.SharedSecret, registrationData.ClientKey, httpCli);

                fields = (JArray)JsonConvert.DeserializeObject(await taskMeasurer.Run(() => cli.Get($"/rest/api/3/field"), "000_FetchFields"));
                var EpicNameField = getField(fields, "com.pyxis.greenhopper.jira:gh-epic-label");
                

                string[] responses = await taskMeasurer.Run(() => Task.WhenAll(issuesList.Select(i => cli.Get($"/rest/api/3/issue/{i}"))), "111_FetchIssuesData");

                PdfReport pdfReport = new PdfReport(filePath: reportFileName, storageName: _configuration.GetValue<string>("Settings:StorageName"), debug: _hostEnvironment.IsDevelopment());
                await pdfReport.Configure(_client.PdfApi, _client.BarcodeApi);

                issues = JToken.FromObject(new
                {
                    issues = responses.Select(JsonConvert.DeserializeObject).ToList()
                }) as JObject;

                // get stories for epics
                epicStories = new Dictionary<string, JArray>();
                if (!string.IsNullOrEmpty(EpicNameField))
                {
                    var issuesArr = await Task.WhenAll(issues["issues"]
                        .Where(i => !string.IsNullOrEmpty(i.SelectToken($"$.fields.{EpicNameField}")?.ToString()))
                        .Select(async i => KeyValuePair.Create(i.SelectToken($"$.key")?.ToString(), await getSubtasksForEpic(cli, i.SelectToken($"$.key")?.ToString()))
                        ));

                    epicStories = issuesArr.ToDictionary(t => t.Key, t => t.Value);
                }

                document = taskMeasurer.RunSync(() =>
                    {
                        reportJiraCloudModel = new ReportJiraCloudModel(System.IO.File.ReadAllText(
                            _configuration.GetValue("Templates:ReportIssuesModel",
                                "template/Report-Issues.Mustache")))
                        {
                            EpicLinkField = getField(fields, "com.pyxis.greenhopper.jira:gh-epic-link"),
                            EpicNameField = EpicNameField,
                            GenerateQRCode = includeQRCode
                        };
                        return reportJiraCloudModel.CreateReportModel(reportJiraCloudModel.issuesModel(issues, epicStories));
                    }, "112_PrepareReportModel");
                    
                if (null == document.Options)
                    document.Options = new Report.Model.DocumentOptions();
                await pdfReport.Report(document);
                

                DateTime current = DateTime.Now;
                reportFile = new Model.ReportFile
                {
                    UniqueId = $"{Guid.NewGuid()}",
                    ReportType = extension,
                    ContentType = "application/pdf",
                    FileName = issuesList.Count == 1 ? $"{issuesList.First()}.{extension}" : $"Issues.{extension}",
                    StorageFileName = reportFileName,
                    ClientId = registrationData.Id,
                    Created = current,
                    Expired = current.AddHours(_configuration.GetValue("Settings:FileExpirationHours", 24)),
                };
                _dbContext.ReportFile.Add(reportFile);
                statItems = taskMeasurer.Stat.Concat(pdfReport.Stat).ToList();
                
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"{current} Finished {registrationData.ClientKey} in {DateTime.Now - requestDateTime}");
                return result = new OkObjectResult(new {
                    fileid = reportFile.UniqueId
                    , downloadlink = _basePathReplacement.ReplaceBaseUrl(Url.Link("GetDownload", new { id = reportFile.UniqueId }))
                    , expText = TimeSpan.FromHours(_configuration.GetValue("Settings:FileExpirationHours", 24)).ToReadableString()
                    , exp = reportFile.Expired
                });
            }
            catch (Exception ex)
            {
                ZipFileArchive archive = new ZipFileArchive().AddFile("010_request_params.json", new
                {
                    RequestId = _client.RequestId,
                    FileId = fileId,
                    FileName = reportFileName,
                    includeQRCode, referer, realIp, userAgent, forwardedFor,
                    issues = issuesList
                });
                foreach (var f in new Dictionary<string, object> {
                    { "020_registration.json", registrationData },
                    { "030_fields.json", fields },
                    { "040_issues.json", issues },
                    { "050_epic_stories.json", epicStories },
                    { "051_RenderedDocument.yaml", reportJiraCloudModel?.RenderedYamlDocument },
                    { "052_RenderedJsonDocument.yaml", reportJiraCloudModel?.RenderedJsonDocument },
                    { "060_document.json", document },
                    { "070_report_file.json", reportFile },
                    { "080_result.json", result },
                }.ToArray())
                    archive.AddFile(f.Key, f.Value);
                throw new ControllerException($"Error generating {reportFileName}", innerException: ex, customData: await archive.Archive());
            }
            finally
            {
                _client.Stat = statItems;
            }
        }
        [HttpGet("download/{id}", Name = "GetDownload")]
        public async Task<ActionResult> GetDownload([FromRoute]string id 
            , [FromHeader(Name = "Referer")]string referer, [FromHeader(Name = "x-real-ip")]string realIp, [FromHeader(Name = "User-Agent")]string userAgent
            , [FromHeader(Name = "x-forwarded-for")]string forwardedFor)
        {
            try
            {
                var file = _dbContext.ReportFile.FirstOrDefault(r => r.UniqueId == id);
                if (default(Model.ReportFile) == file)
                    throw new ControllerException($"Not found {id}", code: HttpStatusCode.NotFound);

                file.Accessed = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                MemoryStream stream = new MemoryStream();
                await using (var s = await _client.PdfApi.DownloadFileAsync(path: file.StorageFileName,
                    storageName: _configuration.GetValue<string>("Settings:StorageName")))
                {
                    await s.CopyToAsync(stream);
                }

                stream.Position = 0;
                return new FileStreamResult(stream, file.ContentType)
                {
                    FileDownloadName = file.FileName
                };
            }
            catch (Aspose.Pdf.Cloud.Sdk.Client.ApiException ex)
            {
                throw new ControllerException($"Error downloading {id}", code: (HttpStatusCode)ex.ErrorCode, innerException: ex);
            }
        }
    }
}