using System;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.App.Middleware;
using Aspose.Cloud.Marketplace.Report;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services
{
    /// <summary>
    /// Client service interface
    /// </summary>
    public interface IAppJiraCloudExporterCli : IAppCustomErrorReportingClient
    {
        ClientRegistration RegistrationData { get; set; }
        Aspose.BarCode.Cloud.Sdk.Interfaces.IBarcodeApi BarcodeApi { get; }
        Aspose.Pdf.Cloud.Sdk.Api.IPdfApi PdfApi { get; }

        string AtlassianAppKey { get; }

        Exception AuthException { get; set; }
    }
}
