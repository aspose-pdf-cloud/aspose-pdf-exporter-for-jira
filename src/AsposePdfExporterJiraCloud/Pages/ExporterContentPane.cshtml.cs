using System;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using Aspose.Cloud.Marketplace.App.Middleware;
using Aspose.Cloud.Marketplace.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using ReporterMiddleware = Aspose.Cloud.Marketplace.App.Middleware.StoreExceptionHandlingMiddleware<Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services.IAppJiraCloudExporterCli>;

// Button glyphs https://docs.atlassian.com/aui/7.9.3/docs/icons.html
namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Pages
{
    [MiddlewareFilter(typeof(Middleware.JiraSilentAuthPipeline))]
    public class ExporterContentPaneModel : PageModel
    {
        private readonly ILogger<ExporterContentPaneModel> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppJiraCloudExporterCli _client;
        public ExporterContentPaneModel(IServiceProvider serviceProvider, ILogger<ExporterContentPaneModel> logger, ILoggerFactory loggerFactory
            , IConfiguration configuration, IAppJiraCloudExporterCli client)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
            _client = client;

            Support = configuration.GetValue("Settings:Support", "support");
            ErrorInfo = null;
        }

        public string ExportToken { get; set; } = "exp_token";
        public string SingleIssueKey { get; set; } = "";
        public bool SingleMode { get; set; } = false;
        public string Support { get; set; }
        public ErrorInfo ErrorInfo { get; set; }
        public bool IsErrorOccured => (ErrorInfo != null);
        public string ExportActionPath { get; set; }
        public string IssuesText { get; set; } = "issues";
        public async Task  OnGet([FromQuery]string issueId, [FromQuery]string issueKey
            , [FromQuery]string xdm_e, [FromQuery]string xdm_c, [FromQuery]string cp, [FromQuery]string lic, [FromQuery]string cv, [FromQuery]string jwt
            , [FromHeader(Name = "Referer")]string referer, [FromHeader(Name = "x-real-ip")]string realIp, [FromHeader(Name = "User-Agent")]string userAgent
            , [FromHeader(Name = "x-forwarded-for")]string forwardedFor
            )
        {
            Model.ClientRegistration registrationData = null;
            try
            {
                // check if middleware has problems with token validation
                if (null != _client.AuthException)
                    throw _client.AuthException;
                ExportActionPath = Url.Action("ExportIssues", "JiraExporter");
                registrationData = _client.RegistrationData;
                ExportToken = Utils.EncodeToken(registrationData.SharedSecret, registrationData.ClientKey,
                    registrationData.Key, 15, "POST"
                    , ExportActionPath, "");
                SingleIssueKey = issueKey;
                SingleMode = (!string.IsNullOrEmpty(issueId));
                if (!string.IsNullOrEmpty(issueKey))
                    IssuesText = $"<b>{issueKey}</b>";
            }
            catch (Exception exc)
            {
                ZipFileArchive archive = new ZipFileArchive().AddFile("010_ExporterContentPane_params.json", new
                {
                    RequestId = _client.RequestId,
                    IssueId = issueId,
                    IssueKey = issueKey,
                    ExportActionPath,
                    ExportToken,
                    registrationData,
                    SingleIssueKey,
                    SingleMode,
                    IssuesText
                });
                // we cannot use exception middleware here, we have to handle errors in place
                (_, ErrorInfo) = await ReporterMiddleware.HandleError(_client
                    , new ControllerException($"Error initializing ExporterContentPane", innerException: exc, customData: await archive.Archive())
                    , HttpContext, _serviceProvider,
                    ReporterMiddleware.HandleReportError, _loggerFactory.CreateLogger<BaseExceptionHandlingMiddleware<IAppJiraCloudExporterCli>>());
            }
        }
    }
}