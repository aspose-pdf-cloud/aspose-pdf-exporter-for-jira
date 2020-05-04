using Microsoft.AspNetCore.Builder;
using System;
using Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter.Middleware;

namespace Aspose.Cloud.Marketplace.App.JiraCloud.Pdf.Exporter
{
    public static class Extensions
    {
        public static IApplicationBuilder UseJiraAuth(this IApplicationBuilder builder, JiraAuthMiddlewareOptions options)
        {
            return builder.UseMiddleware<JiraAuthMiddleware>(options);
        }

        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Duration().Days > 0 ? $"{span.Days} day{(span.Days > 1 ? "s" : "")}, " : string.Empty,
                span.Duration().Hours > 0 ? $"{span.Hours} day{(span.Hours > 1 ? "s" : "")}, " : string.Empty,
                span.Duration().Minutes > 0 ? $"{span.Minutes} day{(span.Minutes > 1 ? "s" : "")}, " : string.Empty,
                span.Duration().Seconds > 0 ? $"{span.Seconds} day{(span.Seconds > 1 ? "s" : "")}, " : string.Empty);

            if (formatted.EndsWith(", ")) formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

            return formatted;
        }
    }
}
