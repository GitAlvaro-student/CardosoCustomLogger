using CustomLogger.Abstractions;
using CustomLogger.HealthChecks.Abstractions;
using CustomLogger.HealthChecks.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.AspNetCore.HealthChecks
{
    /// <summary>
    /// Adapter entre CustomLogger.HealthChecks e ASP.NET Core Health Checks.
    /// 
    /// RESPONSABILIDADE:
    /// - Executar avaliação via <see cref="ILoggingHealthEvaluator"/>
    /// - Mapear <see cref="LoggingHealthReport"/> para <see cref="HealthCheckResult"/>
    /// 
    /// MAPEAMENTO:
    /// - Healthy → Healthy
    /// - Degraded → Degraded
    /// - Unhealthy → Unhealthy
    /// - Unknown → Unhealthy (conservador)
    /// 
    /// PROTEÇÃO:
    /// Nunca lança exceção, retorna Unhealthy em caso de falha.
    /// </summary>
    public sealed class CustomLoggerHealthCheck : IHealthCheck
    {
        private readonly ILoggingHealthEvaluator _evaluator;
        private readonly ILoggingHealthState _healthState;

        /// <summary>
        /// Construtor com injeção de dependências.
        /// </summary>
        /// <param name="evaluator">Avaliador de saúde do logger</param>
        /// <param name="healthState">Estado atual do logger</param>
        /// <exception cref="ArgumentNullException">Se evaluator ou healthState forem null</exception>
        public CustomLoggerHealthCheck(
            ILoggingHealthEvaluator evaluator,
            ILoggingHealthState healthState)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _healthState = healthState ?? throw new ArgumentNullException(nameof(healthState));
        }

        /// <summary>
        /// Executa health check do logger.
        /// 
        /// PROTEÇÃO:
        /// - Nunca lança exceção
        /// - CancellationToken é ignorado (avaliação é síncrona e rápida)
        /// - Em caso de erro inesperado, retorna Unhealthy
        /// </summary>
        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Executar avaliação
                var report = _evaluator.Evaluate(_healthState);

                // Mapear status
                var status = MapStatus(report.Status);

                // Construir data com informações do relatório
                var data = new Dictionary<string, object>
                {
                    { "bufferUsagePercentage", report.BufferUsagePercentage },
                    { "issuesCount", report.Issues.Count },
                    { "sinksCount", report.SinkStatuses.Count },
                    { "evaluatedAtUtc", report.EvaluatedAtUtc }
                };

                // Adicionar status individual dos sinks
                foreach (var sink in report.SinkStatuses)
                {
                    data[$"sink_{sink.Key}"] = sink.Value.ToString();
                }

                // Construir descrição com issues
                var description = BuildDescription(report);

                var result = new HealthCheckResult(
                    status: status,
                    description: description,
                    data: data
                );

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                // Fallback defensivo: nunca propagar exceção
                var result = HealthCheckResult.Unhealthy(
                    description: $"CustomLogger health check failed: {ex.Message}",
                    exception: ex
                );

                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// Mapeia LoggingHealthStatus para HealthStatus do ASP.NET Core.
        /// 
        /// REGRA:
        /// Unknown é mapeado para Unhealthy (conservador).
        /// </summary>
        private HealthStatus MapStatus(LoggingHealthStatus loggingStatus)
        {
            switch (loggingStatus)
            {
                case LoggingHealthStatus.Healthy:
                    return HealthStatus.Healthy;

                case LoggingHealthStatus.Degraded:
                    return HealthStatus.Degraded;

                case LoggingHealthStatus.Unhealthy:
                    return HealthStatus.Unhealthy;

                case LoggingHealthStatus.Unknown:
                    return HealthStatus.Unhealthy; // Conservador

                default:
                    return HealthStatus.Unhealthy; // Fallback
            }
        }

        /// <summary>
        /// Constrói descrição textual do relatório.
        /// 
        /// FORMATO:
        /// - Se Healthy: "Logger is healthy"
        /// - Se problemas: Lista dos issues
        /// </summary>
        private string BuildDescription(LoggingHealthReport report)
        {
            if (report.Issues.Count == 0)
            {
                return "Logger is healthy";
            }

            var issues = report.Issues
                .Select(i => $"[{i.Severity}] {i.Component}: {i.Description}")
                .ToList();

            return string.Join("; ", issues);
        }
    }
}
