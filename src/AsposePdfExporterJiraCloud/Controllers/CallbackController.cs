using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Model;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Services;
using Aspose.Cloud.Marketplace.Services;
using Aspose.Cloud.Marketplace.Services.Model.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AtlModel = Aspose.Cloud.Marketplace.App.Atlassian.Connect.Model;
using AtlConnect = Aspose.Cloud.Marketplace.App.Atlassian.Connect;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Controllers
{
    ///
    /// https://developer.atlassian.com/cloud/jira/platform/app-descriptor/#lifecycle
    /// https://developer.atlassian.com/cloud/confluence/security-for-connect-apps/
    ///
    /// Use Connect inspector to debug events https://connect-inspector.prod.public.atl-paas.net/
    /// 
    [Route("app/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly DbContext.DatabaseContext _dbContext;
        private readonly ILogger<JiraExporterController> _logger;
        private readonly ILoggingService _remoteLogger;
        private readonly IAppJiraCloudExporterCli _client;
        public CallbackController(DbContext.DatabaseContext dbContext, ILogger<JiraExporterController> logger, ILoggingService remoteLogger
            , IAppJiraCloudExporterCli client)
        {
            _dbContext = dbContext;
            _logger = logger;
            _remoteLogger = remoteLogger;
            _client = client;
        }

        protected async Task ReportSetupEvent(ClientRegistration registration, string @event, bool clientProcessed)
        {
            var doc = new ElasticsearchSetupDocument(id: _client.RequestId, logName: "setup_log"
                , appName: _client.AppName, action: @event
                , actionId: registration?.ClientStateId.ToString(), actionOriginator: registration?.ClientKey
                , actionDate: DateTime.Now, subscriber: registration?.BaseUrl
                , message: registration?.Description, path: Request.Path
                , controllerName: ControllerContext.RouteData?.Values["controller"]?.ToString(), actionName: ControllerContext.RouteData?.Values["action"]?.ToString()
                , elapsedSeconds: null, parameters: new Dictionary<string, string>
                {
                    { "client_processed",  clientProcessed.ToString()}
                }, resultCode: 200);
                
            await _remoteLogger.ReportSetupLog(doc);
            
        }

        [HttpPost("installed")]
        [MiddlewareFilter(typeof(Middleware.JiraAnonymousAuthPipeline))]
        public async Task AppInstalled([FromQuery]string userAccountId, [FromBody] AtlModel.ClientRegistrationData data)
        {
            _logger.LogInformation($"AppInstalled client: {data.ClientKey}");
            /* as stated
            *  https://developer.atlassian.com/cloud/confluence/security-for-connect-apps/
            *  First install after being uninstalled:
            *  The shared secret sent in the preceding installed callback. This allows apps to allow the new installation to access previous tenant data (if any exists).
            *  A valid signature demonstrates that the sender is in possession of the shared secret from when the old tenant data was accessed.
            */
            ClientRegistration registration = null;
            // new clients should have _client.RegistrationData set to null
            if (null == _client.RegistrationData)
            {
                registration = await _dbContext.ClientRegistration.Where(c => c.Key == _client.AtlassianAppKey && c.ClientKey == data.ClientKey).FirstOrDefaultAsync();
                if (default(ClientRegistration) != registration)
                    throw new ArgumentException($"Client {data.ClientKey} already registered but no token was provided");
            }
            if (null != _client.AuthException)
                throw new ControllerException(message:$"Exception occired during token validation: {_client.AuthException.Message}", innerException: _client.AuthException);

            // we have checked that either client is new client (no token provided) or already registered (token provided, verified, _client.RegistrationData filled
            //foreach (var r in _dbContext.ClientRegistration.Where(c => c.Key == _client.AtlassianAppKey && c.ClientKey == data.ClientKey))
            //    _dbContext.ClientRegistration.Remove(r);

            if (null != _client.RegistrationData)
            {
                registration = await _dbContext.ClientRegistration.Where(c => c.Id == _client.RegistrationData.Id)
                    .FirstOrDefaultAsync();
                registration.Update(data);
            }
            else
            {
                registration = ClientRegistration.From(data);
                registration.UserAccountId = userAccountId;
                _dbContext.ClientRegistration.Add(registration);
            }
            registration.ClientStateId = (int)AtlConnect.Enum.eRegTypes.AppInstalled;
            await _dbContext.SaveChangesAsync();
            _client.RegistrationData = registration;
            await ReportSetupEvent(registration, "installed", true);
        }

        [HttpPost("uninstalled")]
        [MiddlewareFilter(typeof(Middleware.JiraAuthPipeline))]
        public async Task AppUninstalled([FromBody] AtlModel.ClientRegistrationData data)
        {
            _logger.LogInformation($"AppUninstalled client: {data.ClientKey}");
            // we should already have _client.RegistrationData filled.
            ClientRegistration registration = await _dbContext.ClientRegistration.Where(c => c.Id == _client.RegistrationData.Id).FirstOrDefaultAsync();
            if (default(ClientRegistration) != registration)
            {
                registration.ClientStateId = (int)AtlConnect.Enum.eRegTypes.AppUninstalled;
                await _dbContext.SaveChangesAsync();
            }
            else
                _logger.LogError($"AppUninstalled: Unable to find registered client: {data.ClientKey} with id {_client.RegistrationData?.Id}");
            await ReportSetupEvent(registration ?? ClientRegistration.From(data), "uninstalled", default(ClientRegistration) != registration);
        }

        [HttpPost("enabled")]
        [MiddlewareFilter(typeof(Middleware.JiraAuthPipeline))]
        public async Task AppEnabled([FromBody] AtlModel.ClientRegistrationData data)
        {
            _logger.LogInformation($"AppEnabled client: {data.ClientKey}");
            // we should already have _client.RegistrationData filled.
            ClientRegistration registration = await _dbContext.ClientRegistration.Where(c => c.Id == _client.RegistrationData.Id).FirstOrDefaultAsync();
            if (default(ClientRegistration) != registration)
            {
                registration.ClientStateId = (int)AtlConnect.Enum.eRegTypes.AppEnabled;
                await _dbContext.SaveChangesAsync();
            } else
                _logger.LogError($"AppEnabled: Unable to find registered client: {data.ClientKey} with id {_client.RegistrationData?.Id}");
            await ReportSetupEvent(registration ?? ClientRegistration.From(data), "enabled", default(ClientRegistration) != registration);
        }
        /// <summary>
        /// application disabled callback
        /// we must install JiraAuthMiddleware for this call since it uses Authorization header with JWT token scheme
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("disabled")]
        [MiddlewareFilter(typeof(Middleware.JiraAuthPipeline))]
        public async Task AppDisabled([FromBody] AtlModel.ClientRegistrationData data)
        {
            _logger.LogInformation($"AppDisabled client: {data.ClientKey}");
            // we should already have _client.RegistrationData filled.
            ClientRegistration registration = await _dbContext.ClientRegistration.Where(c => c.Id == _client.RegistrationData.Id).FirstOrDefaultAsync();
            if (default(ClientRegistration) != registration)
            {
                registration.ClientStateId = (int)AtlConnect.Enum.eRegTypes.AppDisabled;
                await _dbContext.SaveChangesAsync();
            }
            else
                _logger.LogError($"AppDisabled: Unable to find registered client: {data.ClientKey} with id {_client.RegistrationData?.Id}");
            await ReportSetupEvent(registration ?? ClientRegistration.From(data), "disabled", default(ClientRegistration) != registration);
        }
    }
}