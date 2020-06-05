using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.Common;
using System.Net;
using System.Text;
using Aspose.BarCode.Cloud.Sdk.Model.Requests;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.Report;
using Aspose.Cloud.Marketplace.Services;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services
{
    /// <summary>
    /// Client service implementation
    /// </summary>
    public class JiraCloudExporterClientService : IAppJiraCloudExporterCli
    {
        internal const string AsposeClientHeaderName = "x-aspose-client";
        internal const string AsposeClientVersionHeaderName = "x-aspose-client-version";

        public string RequestId { get; }
        public string AppName { get; }
        public string AtlassianAppKey { get; }
        public Exception AuthException { get; set; }
        public List<StatisticalDocument> Stat { get; set; }
        public ClientRegistration RegistrationData { get; set; }
        public double? ElapsedSeconds => _stopwatch?.Elapsed.TotalSeconds;

        private readonly string _apiKey, _appSid, _basePath;
        private readonly bool _debug;
        private Aspose.Pdf.Cloud.Sdk.Api.PdfApi _pdfApi;
        private Aspose.BarCode.Cloud.Sdk.Interfaces.IBarcodeApi _barcodeApi;
        private readonly Stopwatch _stopwatch;
        private readonly Aspose.Pdf.Cloud.Sdk.Client.Configuration _pdfConfig;
        private readonly Aspose.BarCode.Cloud.Sdk.Configuration _barcodeConfig;
        public JiraCloudExporterClientService(string appName, string atlassianAppKey,
            string apiKey, string appSid, string basePath = "", bool debug = false)
        {
            _apiKey = apiKey;
            _appSid = appSid;
            _basePath = basePath;
            _debug = debug;
            _pdfApi = null;
            _barcodeApi = null;
            AuthException = null;
            
            RequestId = Guid.NewGuid().ToString();
            AppName = appName;
            AtlassianAppKey = atlassianAppKey;
            Stat = new List<StatisticalDocument>();
            var version = GetType().Assembly.GetName().Version;
            var DefaultHeaders = new Dictionary<string, string>
            {
                {AsposeClientHeaderName, AppName},
                {AsposeClientVersionHeaderName, $"{version.Major}.{version.Minor}"}
            };
            _pdfConfig = new Aspose.Pdf.Cloud.Sdk.Client.Configuration(_apiKey, _appSid)
            {
                DefaultHeader = DefaultHeaders
            };
            if (!string.IsNullOrEmpty(_basePath))
                _pdfConfig.BasePath = _basePath;

            _barcodeConfig = new Aspose.BarCode.Cloud.Sdk.Configuration
            {
                DebugMode = _debug,
                AppKey = _apiKey,
                AppSid = _appSid,
                DefaultHeaders = DefaultHeaders
            };
            if (!string.IsNullOrEmpty(_basePath))
                _barcodeConfig.ApiBaseUrl = _basePath;

            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public Aspose.Pdf.Cloud.Sdk.Api.IPdfApi PdfApi => _pdfApi ??= new Aspose.Pdf.Cloud.Sdk.Api.PdfApi(_pdfConfig);
        
        public Aspose.BarCode.Cloud.Sdk.Interfaces.IBarcodeApi BarcodeApi => _barcodeApi = new Aspose.BarCode.Cloud.Sdk.Api.BarcodeApi(_barcodeConfig);
        
        public ValueTuple<int, string, string, byte[]> ErrorResponseInfo(Exception ex)
        {
            HttpStatusCode code = HttpStatusCode.InternalServerError;
            byte[] customData = null;
            string text = "General error";
            if (null != ex)
            {
                text = ex.Message;
                code = ControllerException.StatusCode(ex);
                if (ex is ControllerException cex)
                {
                    code = cex.Code;
                    customData = cex.CustomData;
                }
            }
            return ((int)code, code.ToString(), text, customData);
        }

        public async Task<string> ReportException(ElasticsearchErrorDocument doc, IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var basePathReplacement = scope.ServiceProvider.GetRequiredService<IBasePathReplacement>();
            var linkGenerator = scope.ServiceProvider.GetRequiredService<LinkGenerator>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext.DatabaseContext>();
            string errorFileId = Guid.NewGuid().ToString();
            string errorFileUrl = null;
            var extension = "json";
            await using (MemoryStream ms =
                new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(doc))))
            {
                string clientKey = RegistrationData?.ClientKey ?? "undefined";
                int clientId = RegistrationData?.Id ?? -1;
                string errorFileStoragePath =
                    $"{configuration.GetValue<string>("Settings:StorageRoot", "clients_jiracloud")}/{clientKey}/{errorFileId}.{extension}";

                var uploadResult = await PdfApi.UploadFileAsync(errorFileStoragePath, ms,
                    configuration.GetValue<string>("Settings:StorageName"));
                if (null != uploadResult.Errors && uploadResult.Errors.Count > 0)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                    logger.LogError(
                        $"Error occured while uploading file {errorFileStoragePath}. Error:{string.Join(";", uploadResult.Errors.Select(e => e.Message))}");
                    errorFileUrl = null;
                }
                else
                {
                    DateTime current = DateTime.Now;
                    var errorFile = new Model.ReportFile
                    {
                        UniqueId = $"{Guid.NewGuid()}",
                        ReportType = extension,
                        ContentType = "application/json",
                        FileName = $"Error-{current:yyyy-MM-dd}.{extension}",
                        StorageFileName = errorFileStoragePath,
                        ClientId = clientId,
                        Created = current,
                        Expired = current.AddHours(configuration.GetValue("Settings:FileExpirationHours", 24)),
                    };
                    dbContext.ReportFile.Add(errorFile);
                    await dbContext.SaveChangesAsync();
                    errorFileUrl = basePathReplacement.ReplaceBaseUrl(
                        linkGenerator.GetUriByAction(ctx, "GetDownload", "JiraExporter", values: new { id = errorFile.UniqueId }));
                }
            }

            return errorFileUrl;
        }

    }
}
