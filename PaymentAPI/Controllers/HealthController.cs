using CustomLogger.HealthChecks;
using CustomLogger.HealthChecks.Models;
using System.Web.Http;

namespace PaymentAPI.Controllers
{
    /// <summary>
    /// Endpoint de health check para .NET Framework WebApi.
    /// </summary>
    public class HealthController : ApiController
    {
        private static readonly LoggingHealthMonitor _monitor = CreateMonitor();

        /// <summary>
        /// GET /api/health
        /// </summary>
        [HttpGet]
        [Route("api/health")]
        public IHttpActionResult Get()
        {
            var report = _monitor.GetLatestReport();

            // Mapear status para código HTTP
            var statusCode = MapStatusCode(report.Status);

            return Content(statusCode, new
            {
                status = report.Status.ToString(),
                bufferUsagePercentage = report.BufferUsagePercentage,
                issuesCount = report.Issues.Count,
                issues = report.Issues,
                sinkStatuses = report.SinkStatuses,
                evaluatedAtUtc = report.EvaluatedAtUtc
            });
        }

        /// <summary>
        /// Mapeia LoggingHealthStatus para HTTP status code.
        /// </summary>
        private System.Net.HttpStatusCode MapStatusCode(LoggingHealthStatus status)
        {
            switch (status)
            {
                case LoggingHealthStatus.Healthy:
                case LoggingHealthStatus.Degraded:
                    return System.Net.HttpStatusCode.OK;

                case LoggingHealthStatus.Unhealthy:
                case LoggingHealthStatus.Unknown:
                    return System.Net.HttpStatusCode.ServiceUnavailable;

                default:
                    return System.Net.HttpStatusCode.InternalServerError;
            }
        }

        /// <summary>
        /// Cria monitor singleton (chamado uma vez no startup).
        /// </summary>
        private static LoggingHealthMonitor CreateMonitor()
        {
            // Obter provider do logger (assumindo registro global)
            var provider = GlobalLoggerProvider.Instance; // Exemplo
            var evaluator = new DefaultLoggingHealthEvaluator();

            return new LoggingHealthMonitor(
                evaluator: evaluator,
                healthState: provider, // Provider implementa ILoggingHealthState
                evaluationIntervalSeconds: 60 // Avaliar a cada 60s
            );
        }
    }
}
