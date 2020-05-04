using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using Aspose.Cloud.Marketplace.App.Atlassian.Connect.Model;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests;
using Aspose.Cloud.Marketplace.App.Middleware;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using AutoFixture;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.IntegrationTests
{
    public class StrictTypeContractResolver : DefaultContractResolver
    {
        private readonly Type _targetType;
        public StrictTypeContractResolver(Type targetType) => _targetType = targetType;
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            => base.CreateProperties
            (
                _targetType.IsAssignableFrom(type) ? _targetType : type,
                memberSerialization
            );
    }

    public class CallbackTests : IClassFixture<IntegrationTestsWebApplicationFactory<Startup>>
    {
        internal IntegrationTestsWebApplicationFactory<Startup> Factory;
        internal readonly ITestOutputHelper Output;
        internal readonly HttpClient Client;
        internal JiraCloudExporterClientServiceMock ClientServiceMock => Factory.AppFixture.ClientServiceMock;
        public CallbackTests(IntegrationTestsWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            Factory = factory;
            Output = output;
            Factory.ClearInvocations();
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        }

        public static async Task<HttpResponseMessage> ExecuteRequest(HttpClient httpCli, HttpRequestMessage request, ClientRegistrationData reg)
        {
            if (reg != null)
            {
                AtlConnect.RestApiClient cli = new AtlConnect.RestApiClient(reg.ClientKey, reg.SharedSecret, reg.Key, httpCli);
                return await cli.Send(request);
            }
            else
                return await httpCli.SendAsync(request);
        }

        public static HttpRequestMessage CreateRequest<T>(string requestUri, HttpMethod method, T content) =>
            CreateRequest(requestUri, method, JsonConvert.SerializeObject(content, new JsonSerializerSettings
                {
                    ContractResolver = new StrictTypeContractResolver(typeof(T))
                }), "application/json");

        public static HttpRequestMessage CreateRequestWithContent(string requestUri, HttpMethod method, HttpContent content) =>
            new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };

        public static HttpRequestMessage CreateRequest(string requestUri, HttpMethod method, string content = null, string mediaType = "application/json") =>
            CreateRequestWithContent(requestUri, method, string.IsNullOrEmpty(content) ? null : new StringContent(content, Encoding.UTF8, mediaType));

        public static async Task<ErrorInfo> GetErrorInfo(HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<ErrorInfo>(await response.Content.ReadAsStringAsync());
        }

        public async Task<ClientRegistration> NewClient()
        {
            ClientRegistration result;
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                result = DatabaseInitialization.CreateFakeClient(context);
                await context.SaveChangesAsync();
            }

            return result;
        }

        [Fact]
        public async void AppInstalled_Test()
        {
            var fixture = new Fixture();
            var regData = fixture.Build<ClientRegistrationData>()
                .With(p => p.BaseUrl, "https://mockjiracloud.com")
                .With(p => p.Key, "appkeymock")
                .Create();

            var request = CreateRequest<ClientRegistrationData>("/app/callback/installed?userAccountId=mock-account-id", HttpMethod.Post, regData);
            var response = await ExecuteRequest(Client, request, null);

            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {await response.Content.ReadAsStringAsync()}");
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                var clientRegistrationData = context.ClientRegistration.First(x => x.ClientKey == regData.ClientKey);
                Assert.Equal(regData.SharedSecret, clientRegistrationData.SharedSecret);
                Assert.Equal((int)AtlConnect.Enum.eRegTypes.AppInstalled, clientRegistrationData.ClientStateId); //uninstalled
            }

            Factory.LoggingMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppInstalled"
                && d.ActionOriginator == regData.ClientKey
            )));
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppInstalled"
                && d.Path == "/app/callback/installed" && d.ResultCode == 200 && d.RequestParameters["userAccountId"] == "mock-account-id"
            )));
        }

        [Fact]
        public async void AppInstalled_PrevClient_Test()
        {
            var cli = await NewClient();
            var fixture = new Fixture();
            var regData = fixture.Build<ClientRegistrationData>()
                .With(p => p.BaseUrl, "https://mockjiracloud.com")
                .With(p => p.Key, "appkeymock")
                .With(p => p.SharedSecret, cli.SharedSecret)
                .Create();
            Trace.WriteLine($"old client {cli.ClientKey}");
            Trace.WriteLine($"new client {regData.ClientKey}");
            var request = CreateRequest<ClientRegistrationData>("/app/callback/installed?userAccountId=mock-account-id", HttpMethod.Post, regData);
            var response = await ExecuteRequest(Client, request, regData);
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {await response.Content.ReadAsStringAsync()}");
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                var clientRegistrationData = context.ClientRegistration.FirstOrDefault(x => x.ClientKey == regData.ClientKey);
                Assert.Equal((int)AtlConnect.Enum.eRegTypes.AppInstalled, clientRegistrationData.ClientStateId);
                Assert.Equal(regData.PublicKey, clientRegistrationData.PublicKey);
            }

            Factory.LoggingMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppInstalled"
                && d.ActionOriginator == regData.ClientKey
            )));
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppInstalled"
                && d.Path == "/app/callback/installed" && d.ResultCode == 200 && d.RequestParameters["userAccountId"] == "mock-account-id"
            )));
        }

        [Fact]
        public async void AppInstalled_PrevClient_Wrong_Secret_Test()
        {
            var fixture = new Fixture();
            // try to add new client that already exists in database
            // new client comes with different SharedSecret
            var regData = fixture.Build<ClientRegistrationData>()
                .With(p => p.BaseUrl, "https://mockjiracloud.com")
                .With(p => p.Key, "appkeymock")
                .With(p => p.ClientKey, ClientServiceMock.RegistrationData.ClientKey)
                .Create();

            var request = CreateRequest("/app/callback/installed?userAccountId=mock-account-id", HttpMethod.Post, regData);
            var response = await ExecuteRequest(Client, request, regData);
            
            Assert.False(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, status code {response.StatusCode}");
            var error = await GetErrorInfo(response);
            Assert.Contains($"Invalid signature for {regData.ClientKey}", error.error_description);
        }

        [Fact]
        public async void AppUninstalled_Test()
        {
            var cli = await NewClient();
            var request = CreateRequest<ClientRegistrationData>("/app/callback/uninstalled", HttpMethod.Post, cli);
            var response = await ExecuteRequest(Client, request, cli);
            
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {await response.Content.ReadAsStringAsync()}");
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                var clientRegistrationData = context.ClientRegistration.FirstOrDefault(x => x.ClientKey == cli.ClientKey);
                Assert.Equal((int)AtlConnect.Enum.eRegTypes.AppUninstalled, clientRegistrationData.ClientStateId); //uninstalled
            }

            Factory.LoggingMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppUninstalled"
                && d.ActionOriginator == cli.ClientKey
            )));
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppUninstalled"
                && d.Path == "/app/callback/uninstalled" && d.ResultCode == 200
            )));
        }

        [Fact]
        public async void AppEnabled_Test()
        {
            var cli = await NewClient();
            var request = CreateRequest<ClientRegistrationData>("/app/callback/enabled", HttpMethod.Post, cli);
            var response = await ExecuteRequest(Client, request, cli);

            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {await response.Content.ReadAsStringAsync()}");
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                var clientRegistrationData = context.ClientRegistration.FirstOrDefault(x => x.ClientKey == cli.ClientKey);
                Assert.Equal((int)AtlConnect.Enum.eRegTypes.AppEnabled, clientRegistrationData.ClientStateId); //uninstalled
            }

            Factory.LoggingMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppEnabled"
                && d.ActionOriginator == cli.ClientKey
            )));
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppEnabled"
                && d.Path == "/app/callback/enabled" && d.ResultCode == 200
            )));
        }


        [Fact]
        public async void AppEnabled_NoToken_Test()
        {
            var request = CreateRequest<ClientRegistrationData>("/app/callback/enabled", HttpMethod.Post, ClientServiceMock.RegistrationData);
            var response = await ExecuteRequest(Client, request, null);

            Assert.False(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, status code {response.StatusCode}");
            var error = await GetErrorInfo(response);
            Assert.Contains("No token", error.error_description);
        }

        [Fact]
        public async void AppEnabled_WrongToken_Test()
        {
            var fixture = new Fixture();
            var wrongReg = fixture.Build<ClientRegistrationData>()
                .With(p => p.BaseUrl, "https://mockjiracloud.com")
                .With(p => p.Key, "appkeymock")
                .Create();

            var request = CreateRequest<ClientRegistrationData>("/app/callback/enabled", HttpMethod.Post, ClientServiceMock.RegistrationData);
            var response = await ExecuteRequest(Client, request, wrongReg);

            Assert.False(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, status code {response.StatusCode}");
            var error = await GetErrorInfo(response);
            Assert.Contains($"Unregistered client {wrongReg.ClientKey}", error.error_description);
        }

        [Fact]
        public async void AppDisabled_Test()
        {
            var cli = await NewClient();
            var request = CreateRequest<ClientRegistrationData>("/app/callback/disabled", HttpMethod.Post, cli);
            var response = await ExecuteRequest(Client, request, cli);
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {await response.Content.ReadAsStringAsync()}");
            await using (var context = Factory.AppFixture.NewDatabaseContext())
            {
                var clientRegistrationData = context.ClientRegistration.FirstOrDefault(x => x.ClientKey == cli.ClientKey);
                Assert.Equal((int)AtlConnect.Enum.eRegTypes.AppDisabled, clientRegistrationData.ClientStateId); //uninstalled
            }

            Factory.LoggingMock.Verify(e => e.ReportSetupLog(It.Is<ElasticsearchSetupDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppDisabled"
                && d.ActionOriginator == cli.ClientKey
            )));
            Factory.LoggingMock.Verify(e => e.ReportAccessLog(It.Is<ElasticsearchAccessLogDocument>(d =>
                d.Id == Factory.AppFixture.ClientServiceMock.RequestId && d.ControllerName == "Callback" && d.ActionName == "AppDisabled"
                && d.Path == "/app/callback/disabled" && d.ResultCode == 200
            )));
        }

        [Fact]
        public async void AppDisabled_NoToken_Test()
        {
            var request = CreateRequest<ClientRegistrationData>("/app/callback/disabled", HttpMethod.Post, ClientServiceMock.RegistrationData);
            var response = await ExecuteRequest(Client, request, null);

            Assert.False(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, status code {response.StatusCode}");
            var error = await GetErrorInfo(response);
            Assert.Contains("No token", error.error_description);
        }

        [Fact]
        public async void AppDisabled_WrongToken_Test()
        {
            var fixture = new Fixture();
            var wrongReg = fixture.Build<ClientRegistrationData>()
                .With(p => p.BaseUrl, "https://mockjiracloud.com")
                .With(p => p.Key, "appkeymock")
                .Create();

            var request = CreateRequest<ClientRegistrationData>("/app/callback/disabled", HttpMethod.Post, ClientServiceMock.RegistrationData);
            var response = await ExecuteRequest(Client, request, wrongReg);

            Assert.False(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, status code {response.StatusCode}");
            var error = await GetErrorInfo(response);
            Assert.Contains($"Unregistered client {wrongReg.ClientKey}", error.error_description);
        }
    }
}
