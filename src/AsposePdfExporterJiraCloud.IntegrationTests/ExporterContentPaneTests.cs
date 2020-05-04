using System;
using System.Linq;
using System.Net.Http;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Tests;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
using AngleSharp;
using AngleSharp.Js;
using AngleSharp.Scripting;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.IntegrationTests
{
    public class ExporterContentPaneTests : IClassFixture<IntegrationTestsWebApplicationFactory<Startup>>
    {
        internal IntegrationTestsWebApplicationFactory<Startup> Factory;
        internal readonly ITestOutputHelper Output;
        internal readonly HttpClient Client;
        internal JiraCloudExporterClientServiceMock ClientServiceMock => Factory.AppFixture.ClientServiceMock;
        public ExporterContentPaneTests(IntegrationTestsWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            Factory = factory;
            Output = output;
            JiraExporterControllerFixture.SetupDownload(ClientServiceMock.PdfApiMock);
            Factory.ClearInvocations();
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }
        public class StandardConsoleLogger : IConsoleLogger
        {
            internal readonly ITestOutputHelper Output;
            public StandardConsoleLogger(ITestOutputHelper output)
            {
                Output = output;
            }
            public void Log(Object[] values)
            {
                var elements = values.Select(m => (m ?? String.Empty).ToString());
                var content = String.Join(", ", elements);
                Output.WriteLine(content);
            }
        }
        [Fact(Skip = "AngleSharp doesn't work with JS nice")]
        public async void ExporterContentPane_Test()
        {
            var request = CallbackTests.CreateRequest("/ExporterContentPane", HttpMethod.Get);
            var response = await CallbackTests.ExecuteRequest(Client, request, ClientServiceMock.RegistrationData);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {responseContent}");
            var document = await HtmlHelpers.GetDocumentAsync(response, new StandardConsoleLogger(Output), responseContent);
            //document.ExecuteScript("body_onload()");
            // Anglesharp.JS supports ES5 https://github.com/AngleSharp/AngleSharp.Js#features but async/await seems like ES8 feature
            var et = document.ExecuteScript("eval(exportToken)");
            //var et = document.ExecuteScript("document.querySelector('#messageDone').innerHTML");
        }


        [Fact(Skip = "AngleSharp doesn't work with JS nice")]
        public async void ExporterContentPane1_Test()
        {
            var request = CallbackTests.CreateRequest("/ExporterContentPane", HttpMethod.Get);
            var response = await CallbackTests.ExecuteRequest(Client, request, ClientServiceMock.RegistrationData);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage.RequestUri} failed, reason {responseContent}");


            var service = new JsScriptingService();
            var cfg = Configuration.Default.With(service).WithConsoleLogger(context => new StandardConsoleLogger(Output));
            var document = await BrowsingContext.New(cfg).OpenAsync(m => m.Content(responseContent));
            //var body_onload = service.GetOrCreateJint(document).GetValue("body_onload");
            //body_onload.Invoke();
            var token = service.GetOrCreateJint(document).GetValue("exportToken");
        }

        [Fact(Skip = "AngleSharp doesn't work with JS nice")]
        public async void BodyOnLoad_Test()
        {
            string responseContent =
                @"<html>
<head>
<script>
var someVar = 'var value';
function body_onload() {
  console.log('hello')
}
</script>
</head>
<body onload='body_onload()'>
  some text
</body>
</html>";
            var service = new JsScriptingService();
            var cfg = Configuration.Default.With(service).WithConsoleLogger(context => new StandardConsoleLogger(Output));
            //var cfg = Configuration.Default.WithJs().WithConsoleLogger(context => new StandardConsoleLogger(Output));
            var document = await BrowsingContext.New(cfg).OpenAsync(m => m.Content(responseContent));
            //service.GetOrCreateJint(document).Execute("body_onload");
            var body_onload = service.GetOrCreateJint(document).GetValue("body_onload");
            var result = body_onload.Invoke();
            var someVar = service.GetOrCreateJint(document).GetValue("someVar");
        }
    }
}
