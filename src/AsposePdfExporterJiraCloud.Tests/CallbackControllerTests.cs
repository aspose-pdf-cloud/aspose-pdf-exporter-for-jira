using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect.Model;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Controllers;
using Aspose.Cloud.Marketplace.Services;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using AutoFixture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests
{

    public class CallbackControllerTests : IClassFixture<ControllerFixture<CallbackController>>
    {
        internal ControllerFixture<CallbackController> Fixture;
        internal ITestOutputHelper Output;
        internal CallbackController Controller => Fixture.Controller;
        internal JiraCloudExporterClientServiceMock ClientServiceMock => Fixture.ClientServiceMock;
        internal Mock<ILoggingService> LoggingServiceMock => Fixture.LoggingServiceMock;

        public CallbackControllerTests(ControllerFixture<CallbackController> fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
            Fixture.ClearInvocations();
        }

        public static ControllerContext CreateControllerContext(Dictionary<string, string> routes) =>
            new ControllerContext(new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new RouteData(new RouteValueDictionary(routes)),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            });

        /// <summary>
        /// Test installation
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AppInstalled_Test()
        {
            var fixture = new Fixture();
            var regData = fixture.Create<ClientRegistrationData>();

            Controller.ControllerContext = CreateControllerContext(new Dictionary<string, string>
            {
                {"action", "app-fake-installed"},
                {"controller", "callback-fake-controller"}
            });

            await Controller.AppInstalled("fake-account-id", regData);

            await using var context = Fixture.NewDatabaseContext();
            var clientRegistrationData =
                context.ClientRegistration.FirstOrDefault(x => x.SharedSecret == regData.SharedSecret);
            Assert.NotNull(clientRegistrationData);
            Assert.Equal(regData.BaseUrl, clientRegistrationData.BaseUrl);
            LoggingServiceMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == ClientServiceMock.RequestId && d.ActionOriginator == regData.ClientKey &&
                d.ControllerName == "callback-fake-controller" &&
                d.ActionName == "app-fake-installed"
            )));
        }

        /// <summary>
        /// Test uninstallation
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AppUninstalled_Test()
        {
            Controller.ControllerContext = CreateControllerContext(new Dictionary<string, string>
            {
                {"action", "app-fake-uninstalled"},
                {"controller", "callback-fake-controller"}
            });
            await Controller.AppUninstalled(ClientServiceMock.RegistrationData);
            await using (var context = Fixture.NewDatabaseContext())
            {
                var clientRegistrationData =
                    context.ClientRegistration.FirstOrDefault(x => x.SharedSecret == ClientServiceMock.RegistrationData.SharedSecret);
                Assert.Equal(2, clientRegistrationData.ClientStateId); //uninstalled
            }

            LoggingServiceMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == ClientServiceMock.RequestId && d.ActionOriginator == ClientServiceMock.RegistrationData.ClientKey &&
                d.ControllerName == "callback-fake-controller" &&
                d.ActionName == "app-fake-uninstalled"
            )));
        }

        /// <summary>
        /// Test enabling
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AppEnabled_Test()
        {
            Controller.ControllerContext = CreateControllerContext(new Dictionary<string, string>
            {
                {"action", "app-fake-enabled"},
                {"controller", "callback-fake-controller"}
            });
            await Controller.AppEnabled(ClientServiceMock.RegistrationData);
            await using (var context = Fixture.NewDatabaseContext())
            {
                var clientRegistrationData =
                    context.ClientRegistration.FirstOrDefault(x => x.SharedSecret == ClientServiceMock.RegistrationData.SharedSecret);
                Assert.Equal(3, clientRegistrationData.ClientStateId); //uninstalled
                LoggingServiceMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                    d.Id == ClientServiceMock.RequestId && d.ActionOriginator == ClientServiceMock.RegistrationData.ClientKey &&
                    d.ControllerName == "callback-fake-controller" &&
                    d.ActionName == "app-fake-enabled"
                )));
            }
        }

        /// <summary>
        /// Test disabling
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AppDisabled_Test()
        {
            Controller.ControllerContext = CreateControllerContext(new Dictionary<string, string>
            {
                {"action", "app-fake-disabled"},
                {"controller", "callback-fake-controller"}
            });
            await Controller.AppDisabled(ClientServiceMock.RegistrationData);
            await using (var context = Fixture.NewDatabaseContext())
            {
                var clientRegistrationData =
                    context.ClientRegistration.FirstOrDefault(x => x.SharedSecret == ClientServiceMock.RegistrationData.SharedSecret);
                Assert.Equal(4, clientRegistrationData.ClientStateId); //uninstalled
                LoggingServiceMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                    d.Id == ClientServiceMock.RequestId && d.ActionOriginator == ClientServiceMock.RegistrationData.ClientKey &&
                    d.ControllerName == "callback-fake-controller" &&
                    d.ActionName == "app-fake-disabled"
                )));
            }
        }
    }
}
