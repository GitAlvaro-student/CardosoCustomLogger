using CustomLogger.HealthChecks;
using CustomLogger.HealthChecks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static PaymentAPI.WebApiApplication;

namespace PaymentAPI
{
    /// <summary>
    /// HTTP Handler para health check em .NET Framework.
    /// 
    /// CONFIGURAÇÃO (Web.config):
    /// <system.webServer>
    ///   <handlers>
    ///     <add name="HealthCheck" path="health" verb="GET" 
    ///          type="PaymentAPI.HealthCheckHandler, PaymentAPI" />
    ///   </handlers>
    /// </system.webServer>
    /// </summary>
    public class HealthCheckHandler : IHttpHandler
    {
        private static readonly LoggingHealthMonitor _monitor = CreateMonitor();

        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            var report = _monitor.GetLatestReport();

            // Mapear status para código HTTP
            context.Response.StatusCode = MapStatusCode(report.Status);
            context.Response.ContentType = "application/json";

            // Serializar resposta (usando Newtonsoft.Json ou similar)
            var json = SerializeReport(report);
            context.Response.Write(json);
        }

        private int MapStatusCode(LoggingHealthStatus status)
        {
            switch (status)
            {
                case LoggingHealthStatus.Healthy:
                case LoggingHealthStatus.Degraded:
                    return 200;

                case LoggingHealthStatus.Unhealthy:
                case LoggingHealthStatus.Unknown:
                    return 503;

                default:
                    return 500;
            }
        }

        private string SerializeReport(LoggingHealthReport report)
        {
            // Exemplo simples (usar JSON serializer real em produção)
            return string.Format(
                "{{\"status\":\"{0}\",\"bufferUsage\":{1},\"issuesCount\":{2},\"evaluatedAt\":\"{3}\"}}",
                report.Status,
                report.BufferUsagePercentage,
                report.Issues.Count,
                report.EvaluatedAtUtc.ToString("o")
            );
        }

        private static LoggingHealthMonitor CreateMonitor()
        {
            var provider = Global.LoggerProvider;
            var evaluator = new DefaultLoggingHealthEvaluator();

            return new LoggingHealthMonitor(
                evaluator: evaluator,
                healthState: provider,
                evaluationIntervalSeconds: 60
            );
        }
    }
}
