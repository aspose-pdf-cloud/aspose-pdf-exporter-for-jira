using System;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model
{
    public class ReportFile
    {
        public int Id { get; set; }
        public string UniqueId { get; set; }
        /// <summary>
        /// pdf, xlsx, docx, etc
        /// </summary>
        public string ReportType { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string StorageFileName { get; set; }
        public string StorageFolder { get; set; }
        public long FileSize { get; set; }
        public int ClientId { get; set; }
        public ClientRegistration Client { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Accessed { get; set; }
        public DateTime? Expired { get; set; }
    }
}
