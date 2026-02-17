using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using CustomLogger.Abstractions;
using CustomLogger.HealthChecks.Abstractions;

namespace CustomLogger.AspNetCore.HealthChecks
{
    /// <summary>
    /// Extensões para registrar CustomLogger health check em ASP.NET Core.
    /// </summary>
    public static class HealthChecksBuilderExtensions
    {
        /// <summary>
        /// Adiciona health check do CustomLogger.
        /// 
        /// PRÉ-REQUISITOS:
        /// - <see cref="ILoggingHealthEvaluator"/> deve estar registrado no DI
        /// - <see cref="ILoggingHealthState"/> deve estar registrado no DI
        ///   (tipicamente via cast de ILoggerProvider)
        /// 
        /// USO:
        /// <code>
        /// services.AddHealthChecks()
        ///         .AddCustomLogger();
        /// </code>
        /// 
        /// OPÇÕES:
        /// - name: Nome do health check (padrão: "customlogger")
        /// - failureStatus: Status em caso de falha (padrão: Unhealthy)
        /// - tags: Tags para filtragem (padrão: ["logging"])
        /// </summary>
        /// <param name="builder">Builder de health checks</param>
        /// <param name="name">Nome do health check</param>
        /// <param name="failureStatus">Status em caso de falha</param>
        /// <param name="tags">Tags para filtragem</param>
        /// <returns>Builder para encadeamento</returns>
        /// <exception cref="ArgumentNullException">Se builder for null</exception>
        public static IHealthChecksBuilder AddCustomLogger(
            this IHealthChecksBuilder builder,
            string name = "customlogger",
            HealthStatus? failureStatus = null,
            string[] tags = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Add(new HealthCheckRegistration(
                name: name ?? "customlogger",
                factory: sp => new CustomLoggerHealthCheck(
                    evaluator: sp.GetRequiredService<ILoggingHealthEvaluator>(),
                    healthState: sp.GetRequiredService<ILoggingHealthState>()
                ),
                failureStatus: failureStatus,
                tags: tags ?? new[] { "logging" }
            ));
        }
    }
}
