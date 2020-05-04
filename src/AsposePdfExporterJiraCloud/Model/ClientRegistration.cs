using System;
using AtlModel = Aspose.Cloud.Marketplace.App.Atlassian.Connect.Model;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model
{
    /// <summary>
    /// Model class represents client registration data
    /// </summary>
    public partial class ClientRegistration : AtlModel.ClientRegistrationData
    {
        public ClientRegistration()
        {
        }
        public static ClientRegistration From(AtlModel.ClientRegistrationData data) => new ClientRegistration().Update(data);
        
        public ClientRegistration Update(AtlModel.ClientRegistrationData data)
        {
            Key = data.Key;
            ClientKey = data.ClientKey;
            PublicKey = data.PublicKey;
            SharedSecret = data.SharedSecret;
            ServerVersion = data.ServerVersion;
            PluginsVersion = data.PluginsVersion;
            BaseUrl = data.BaseUrl;
            ProductType = data.ProductType;
            Description = data.Description;
            EventType = data.EventType;
            return this;
        }
        public int Id { get; set; }

        public string UserAccountId { get; set; }

        public int ClientStateId { get; set; }
        public ClientState ClientState { get; set; }
        public DateTime? Created { get; set; }
        public bool Ok => ClientStateId == (int)AtlConnect.Enum.eRegTypes.AppInstalled || ClientStateId == (int)AtlConnect.Enum.eRegTypes.AppEnabled;
    }
}
