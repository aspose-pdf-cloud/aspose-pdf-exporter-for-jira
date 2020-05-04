using System.Collections.Generic;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model
{
    public class ClientState
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public ICollection<ClientRegistration> ClientRegistrations { get; set; }
    }
}
