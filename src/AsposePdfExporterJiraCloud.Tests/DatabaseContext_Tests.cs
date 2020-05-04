using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Xunit;
using AutoFixture;
using System.Linq;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class DatabaseContext_Tests
    {
        [Fact]
        public void ClientRegistration_Test()
        {
            var (dbName, fakeClient) = DatabaseInitialization.InitializeNewDatabase();
            using var context = DatabaseInitialization.NewDatabaseContext(dbName: dbName);
            Assert.Equal(1, context.ClientRegistration.Count());
            Assert.Equal(fakeClient.UserAccountId, context.ClientRegistration.First().UserAccountId);
        }

        [Fact]
        public void ReportFile_Test()
        {
            ReportFile report = null;
            var (dbName, fakeClient) = DatabaseInitialization.InitializeNewDatabase();
            using (var context = DatabaseInitialization.NewDatabaseContext(dbName: dbName))
            {
                var fixture = new Fixture();
                report = fixture.Build<ReportFile>()
                    .With(p => p.ClientId, fakeClient.Id)
                    .Without(p => p.Client)
                    .Create();
                context.ReportFile.Add(report);
                context.SaveChanges();
            }

            using (var context = DatabaseInitialization.NewDatabaseContext(dbName: dbName))
            {
                Assert.Equal(1, context.ReportFile.Count());
                Assert.Equal(report.StorageFileName, context.ReportFile.First().StorageFileName);
            }
        }
    }
}
