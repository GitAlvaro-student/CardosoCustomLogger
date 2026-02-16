using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomLogger.HealthChecks.Models
{
    /// <summary>
    /// Relatório de saúde do sistema de logging.
    /// 
    /// DESIGN:
    /// - Imutável (readonly struct ou class com readonly properties)
    /// - Serializável (apenas primitivos e coleções básicas)
    /// - Thread-safe por imutabilidade
    /// 
    /// IMPORTANTE:
    /// Representa snapshot pontual da saúde do logger.
    /// Não mantém referência a objetos internos.
    /// </summary>
    public sealed class LoggingHealthReport
    {
        /// <summary>
        /// Status global de saúde.
        /// </summary>
        public LoggingHealthStatus Status { get; }

        /// <summary>
        /// Lista de problemas detectados.
        /// 
        /// Vazia se Status = Healthy.
        /// Contém detalhes se Status = Degraded ou Unhealthy.
        /// </summary>
        public IReadOnlyList<HealthIssue> Issues { get; }

        /// <summary>
        /// Tempo gasto para avaliar o estado (em milissegundos).
        /// 
        /// USO: Detectar se próprio HealthCheck está lento.
        /// Target: &lt; 100ms
        /// </summary>
        public double EvaluationDurationMs { get; }

        /// <summary>
        /// Timestamp da avaliação (UTC).
        /// </summary>
        public DateTime EvaluatedAtUtc { get; }

        /// <summary>
        /// Construtor principal.
        /// </summary>
        public LoggingHealthReport(
            LoggingHealthStatus status,
            IReadOnlyList<HealthIssue> issues,
            double evaluationDurationMs,
            DateTime evaluatedAtUtc)
        {
            Status = status;
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            EvaluationDurationMs = evaluationDurationMs;
            EvaluatedAtUtc = evaluatedAtUtc;
        }

        /// <summary>
        /// Factory method: Cria relatório Healthy.
        /// </summary>
        public static LoggingHealthReport Healthy(double evaluationDurationMs = 0)
        {
            return new LoggingHealthReport(
                status: LoggingHealthStatus.Healthy,
                issues: Array.Empty<HealthIssue>(),
                evaluationDurationMs: evaluationDurationMs,
                evaluatedAtUtc: DateTime.UtcNow
            );
        }

        /// <summary>
        /// Factory method: Cria relatório Unknown (usado em falhas).
        /// </summary>
        public static LoggingHealthReport Unknown(string errorMessage, double evaluationDurationMs = 0)
        {
            var issue = new HealthIssue(
                component: "Evaluator",
                severity: LoggingHealthStatus.Unknown,
                description: errorMessage
            );

            return new LoggingHealthReport(
                status: LoggingHealthStatus.Unknown,
                issues: new[] { issue },
                evaluationDurationMs: evaluationDurationMs,
                evaluatedAtUtc: DateTime.UtcNow
            );
        }

        /// <summary>
        /// Representação textual do relatório.
        /// </summary>
        public override string ToString()
        {
            if (Issues.Count == 0)
            {
                return $"[{Status}] No issues detected";
            }

            var issuesSummary = string.Join("; ", Issues.Select(i => i.ToString()));
            return $"[{Status}] {Issues.Count} issue(s): {issuesSummary}";
        }
    }
}
