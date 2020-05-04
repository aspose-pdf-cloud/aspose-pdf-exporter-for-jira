using System.Collections.Generic;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{
    public class PageFixture : AppEnvironmentFixture
    {
        public PageFixture()
        {
            Configuration = new Dictionary<string, string>
            {
                {"Settings:Support", "support@mock.com"}
            };
        }
    }

    public class ExporterContentPaneTests : IClassFixture<PageFixture>
    {
        internal PageFixture Fixture;
        internal ITestOutputHelper Output;
        public ExporterContentPaneTests(PageFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
            Fixture.ClearInvocations();
        }

        public static PageContext CreateContext(Dictionary<string, string> routes) =>
            new PageContext(new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(new RouteValueDictionary(routes)),
                ActionDescriptor = new PageActionDescriptor()
            });

        [Fact]
        public async Task ExporterContentPane_Test()
        {
            var model = ActivatorUtilities.CreateInstance<ExporterContentPaneModel>(Fixture.ProvideServices(new ServiceCollection()).BuildServiceProvider());
            model.Url = Fixture.UrlHelperMock.Object;
            
            await model.OnGet("10011", "DK-1", "", "", "", "", "", "", "", "", "", "");

            Assert.Equal("support@mock.com", model.Support);
            Assert.Equal("DK-1", model.SingleIssueKey);
            Assert.True(model.SingleMode);
            Assert.Contains("DK-1", model.IssuesText);
            Assert.NotEmpty(model.ExportToken);
            Assert.Equal("/mock/export/action", model.ExportActionPath);
        }

        [Fact]
        public async Task ExporterContentPane_ErrorTest()
        {
            var inconsistentFixture = new AppEnvironmentFixture();
            inconsistentFixture.ClientServiceMock.RegistrationData = null;

            var model = ActivatorUtilities.CreateInstance<ExporterContentPaneModel>(inconsistentFixture.ProvideServices(new ServiceCollection()).BuildServiceProvider());
            model.Url = Fixture.UrlHelperMock.Object;
            model.PageContext = CreateContext(new Dictionary<string, string>
            {
                {"page", "fake_page"}
            });
            await model.OnGet("10011", "DK-1", "", "", "", "", "", "", "", "", "", "");
            Assert.Equal("mock_exception", model.ErrorInfo.error);
            Assert.Equal("Error initializing ExporterContentPane", model.ErrorInfo.error_description);
            Assert.Equal("error_log.zip", model.ErrorInfo.error_result);
            Assert.Matches("mock_[-a-zA-Z0-9]*", model.ErrorInfo.request_id);
        }
    }
}
