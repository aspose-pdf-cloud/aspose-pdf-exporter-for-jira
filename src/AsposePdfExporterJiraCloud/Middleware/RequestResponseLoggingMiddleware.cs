using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Middleware
{
    /// <summary>
    /// Logs requests and responses. Used in dev env only
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next,
                                                ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory
                      .CreateLogger<RequestResponseLoggingMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation($"\n>> Request: {await FormatRequest(context.Request)}\n");

            var originalBodyStream = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                _logger.LogInformation($"\n<< Response: {await FormatResponse(context.Response)}\n");
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            var body = request.Body;
            
            var injectedRequestStream = new MemoryStream();

            await request.Body.CopyToAsync(injectedRequestStream);
            injectedRequestStream.Position = 0;
            string content = "";
            using (var sr = new StreamReader(injectedRequestStream, null, true, -1, true))
            {
                content = await sr.ReadToEndAsync();
            }
            injectedRequestStream.Position = 0;
            request.Body = injectedRequestStream;

            return $"{request.Scheme} {request.Host}{request.Path} {request.QueryString}\n{content}";
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return $"'{text}'";
        }
    }
}
