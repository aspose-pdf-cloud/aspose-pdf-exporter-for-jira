using System;
using System.Linq;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.DbContext;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using AutoFixture;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public static class DatabaseInitialization
    {
        public static DatabaseContext NewDatabaseContext(string dbName) => AppEnvironmentFixture.NewDatabaseContext(dbName);

        /// <summary>
        /// Initializes database, creates and returns ClientRegistration
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static ClientRegistration CreateFakeClient(DatabaseContext context)
        {
            ClientRegistration result = null;
            if (!context.ClientState.Any())
            {
                context.ClientState.Add(new ClientState {Id = 1, Text = "app-installed"});
                context.ClientState.Add(new ClientState {Id = 2, Text = "app-uninstalled"});
                context.ClientState.Add(new ClientState {Id = 3, Text = "app-enabled"});
                context.ClientState.Add(new ClientState {Id = 4, Text = "app-disabled"});
            }
            var fixture = new Fixture();
            /*
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            */
            result = fixture.Build<ClientRegistration>()
                .Without(p => p.ClientState)
                //.Without(p => p.Deleted)
                .With(p => p.ClientStateId, 1)
                .With(p => p.BaseUrl, "https://mockjiracloud.com")
                .With(p => p.Key, "appkeymock")
                .Create();
            context.ClientRegistration.Add(result);
            context.SaveChanges();
            return result;
        }

        public static (string, ClientRegistration) InitializeNewDatabase()
        {
            string dbName = Guid.NewGuid().ToString();
            using var context = NewDatabaseContext(dbName);
            return (dbName, CreateFakeClient(context));
        }
    }
}
