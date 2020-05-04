using CronScheduler.Extensions.Scheduler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Job
{
    /// <summary>
    /// Removes outdated files from storage
    /// </summary>
    public class RemoveReportJob : IScheduledJob
    {
        private readonly ILogger<RemoveReportJob> _logger;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _provider;
        private bool _running;
        public RemoveReportJob(ILogger<RemoveReportJob> logger, IWebHostEnvironment hostEnvironment
            , IConfiguration configuration, IServiceProvider provider)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;
            _provider = provider;
            _running = false;
        }

        public string Name => "RemoveReportJob";

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if(_running)
            {
                _logger.LogInformation($"{DateTime.Now} > Already running {nameof(RemoveReportJob)}");
                return;
            }
            _running = true;
            _logger.LogInformation($"{DateTime.Now} > Running {nameof(RemoveReportJob)}");
            try
            {
                using (var scope = _provider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DbContext.DatabaseContext>();
                    var client = scope.ServiceProvider.GetRequiredService<Services.IAppJiraCloudExporterCli>();
                    var reports = dbContext.ReportFile.Where(r => r.Expired < DateTime.Now);
                    if (reports.Count() > 0)
                    {
                        _logger.LogInformation($"to delete {reports.Count()} records");
                        List<Model.ReportFile> filesToRemove = new List<Model.ReportFile>();
                        foreach (var report in reports)
                        {
                            try
                            {
                                string folder = null, fileName = null;
                                var m = Regex.Match(report.StorageFileName, "^(.*)/([^/]*)$");
                                if (m.Success)
                                {
                                    folder = m.Groups.Count > 1 ? m.Groups[1].Value : "";
                                    fileName = m.Groups.Count > 2 ? m.Groups[2].Value : "";
                                }
                                // Remove file and temporary folder
                                var storageName = _configuration.GetValue<string>("Settings:StorageName");
                                await client.PdfApi.DeleteFileAsync(report.StorageFileName, storageName);
                                if (!string .IsNullOrEmpty(folder) && !string.IsNullOrEmpty(fileName))
                                    await client.PdfApi.DeleteFolderAsync($"{folder}/tmp_{fileName}", storageName, true);

                                _logger.LogInformation($"File {report.StorageFileName} removed from storage");
                                filesToRemove.Add(report);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error removing file {report.StorageFileName}");
                            }
                        }
                        if (filesToRemove.Count > 0)
                        {
                            dbContext.ReportFile.RemoveRange(filesToRemove);
                            dbContext.SaveChanges();
                            _logger.LogInformation($"Removed {filesToRemove.Count} records from context");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error running {nameof(RemoveReportJob)}");
            }
            _running = false;
        }
    }
}
