using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using Aspose.Cloud.Marketplace.Common;
using Aspose.Cloud.Marketplace.Report;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using Aspose.Pdf.Cloud.Sdk.Api;
using Moq;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class JiraCloudExporterClientServiceMock : IAppJiraCloudExporterCli
    {
        public Mock<IPdfApi> PdfApiMock;
        public Mock<IBarcodeApi> BarcodeApiMock;

        public JiraCloudExporterClientServiceMock(Mock<IPdfApi> pdfApiMock = null, Mock<IBarcodeApi> barcodeApiMock = null)
        {
            ElapsedSeconds = 1;
            PdfApiMock = pdfApiMock;
            BarcodeApiMock = barcodeApiMock;
            RequestId = $"mock_{Guid.NewGuid()}";
        }

        public string RequestId { get; }
        public string AppName => "appmock";
        public string AtlassianAppKey => "appkeymock";
        public Exception AuthException { get; set; }
        public double? ElapsedSeconds { get; set; }

        public List<StatisticalDocument> Stat { get; set; }
        public ValueTuple<int, string, string, byte[]> ErrorResponseInfo(Exception ex)
        {
            return ((int)HttpStatusCode.InternalServerError, "mock_exception", ex.Message, null);
        }

        public IPdfApi PdfApi => PdfApiMock.Object;

        public IBarcodeApi BarcodeApi => BarcodeApiMock.Object;

        public async Task<string> ReportException(ElasticsearchErrorDocument doc, IServiceProvider serviceProvider)
        {
            return await Task.FromResult("error_log.zip");
        }

        public ClientRegistration RegistrationData { get; set; }


        public void ClearInvocations()
        {
            PdfApiMock.Invocations.Clear();
            BarcodeApiMock.Invocations.Clear();
        }
    }
}
