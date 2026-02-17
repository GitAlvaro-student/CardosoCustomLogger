using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace GamesAPI
{
    public static class CustomHealthResponses
    {
        public static Task WriteMinimalResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 :
                                         report.Status == HealthStatus.Degraded ? 429 : 503;
            return context.Response.WriteAsync(report.Status.ToString());
        }

        public static Task WriteDetailedJsonResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new {
                        status = kvp.Value.Status.ToString(),
                        description = kvp.Value.Description,
                        durationMs = kvp.Value.Duration.TotalMilliseconds,
                        data = kvp.Value.Data
                    })
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.SerializeAsync(context.Response.Body, payload, options);
        }
    }
}
