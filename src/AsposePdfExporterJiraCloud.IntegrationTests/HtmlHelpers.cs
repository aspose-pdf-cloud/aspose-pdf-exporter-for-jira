using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using AngleSharp.Js;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.IntegrationTests
{
    /// Original: https://github.com/aspnet/AspNetCore.Docs/blob/master/aspnetcore/test/integration-tests/samples/2.x/IntegrationTestsSample/tests/RazorPagesProject.Tests/Helpers/HtmlHelpers.cs
    public static class HtmlHelpers
    {
        public static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response, IConsoleLogger logger, string responseContent = null)
        {
            var content = responseContent ?? await response.Content.ReadAsStringAsync();
            var config = Configuration.Default
                .WithEventLoop()
                .WithDefaultLoader(new LoaderOptions()
                {
                    IsResourceLoadingEnabled = true
                })
                .WithJs()
                .WithCss()
                ;
            if (null != logger)
                config = config.WithConsoleLogger(context => logger);
            var document = await BrowsingContext.New(config)
                .OpenAsync(ResponseFactory, CancellationToken.None)
                //.WaitUntilAvailable()
                ;
            return (IHtmlDocument)document;

            void ResponseFactory(VirtualResponse htmlResponse)
            {
                htmlResponse
                    .Address(response.RequestMessage.RequestUri)
                    .Status(response.StatusCode);

                MapHeaders(response.Headers);
                MapHeaders(response.Content.Headers);

                htmlResponse.Content(content);

                void MapHeaders(HttpHeaders headers)
                {
                    foreach (var header in headers)
                    {
                        foreach (var value in header.Value)
                        {
                            htmlResponse.Header(header.Key, value);
                        }
                    }
                }
            }
        }
    }
}
