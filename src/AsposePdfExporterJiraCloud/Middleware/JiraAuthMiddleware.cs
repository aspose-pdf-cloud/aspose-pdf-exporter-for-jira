using Aspose.Cloud.Marketplace.App.Atlassian.Connect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Middleware
{
    /// <summary>
    /// helper class for MiddlewareFilter attribute
    /// https://github.com/dotnet/AspNetCore.Docs/blob/master/aspnetcore/mvc/controllers/filters.md#using-middleware-in-the-filter-pipeline
    /// </summary>
    public class JiraAuthPipeline
    {
        public void Configure(IApplicationBuilder applicationBuilder) =>
            applicationBuilder.UseJiraAuth(new JiraAuthMiddlewareOptions());
    }
    /// <summary>
    /// Helper class to use JiraAuthMiddleware without throwing any exception
    /// </summary>
    public class JiraSilentAuthPipeline
    {
        public void Configure(IApplicationBuilder applicationBuilder) =>
            applicationBuilder.UseJiraAuth(new JiraAuthMiddlewareOptions(throwException:false));
    }
    /// <summary>
    /// Helper class to use JiraAuthMiddleware without token requirement
    /// </summary>
    public class JiraAnonymousAuthPipeline
    {
        public void Configure(IApplicationBuilder applicationBuilder) =>
            applicationBuilder.UseJiraAuth(new JiraAuthMiddlewareOptions(requireToken:false));
    }
    /// <summary>
    /// Checks and validates JWT token according options
    /// </summary>

    public class JiraAuthMiddlewareOptions
    {
        public bool TokenRequired { get; set; }
        public bool ThrowException { get; set; }
        public JiraAuthMiddlewareOptions(bool requireToken = true, bool throwException=true)
        {
            TokenRequired = requireToken;
            ThrowException = throwException;
        }

    }

    /// <summary>
    /// used to verify JWT tokens and populate IAppJiraCloudExporterCli::RegistrationData upon success
    /// </summary>
    public class JiraAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JiraAuthMiddlewareOptions _options;
        public JiraAuthMiddleware(RequestDelegate next, JiraAuthMiddlewareOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context, DbContext.DatabaseContext dbContext, IAppJiraCloudExporterCli client)
        {
            client.RegistrationData = null;
            try
            {
                var requestValidation = new RequestValidation(context.Request);
                if (requestValidation.TokenExists)
                {
                    var registrationData = dbContext.ClientRegistration.FirstOrDefault(r =>
                        r.Key == client.AtlassianAppKey && r.ClientKey == requestValidation.ClientKey
                    );
                    // if registration data is not found and it is allowed for the request to be unauthorized, pass it through
                    if (default(Model.ClientRegistration) == registrationData && _options.TokenRequired)
                    {
                        throw new Exception($"Unregistered client {requestValidation.ClientKey}");
                    }

                    // if registration data is found, request has to be authorized
                    if (default(Model.ClientRegistration) != registrationData &&
                        requestValidation.Validate(registrationData))
                        client.RegistrationData = registrationData;
                }
                else if (_options.TokenRequired)
                    throw new Exception($"No token");
            }
            catch (Exception ex)
            {
                if (_options.ThrowException)
                    throw;
                client.AuthException = ex;
            }

            await _next(context);
        }
    }
}
