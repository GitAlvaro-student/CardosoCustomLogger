using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.HealthChecks.Models
{
    /// <summary>
    /// Representa um problema detectado durante avaliação de saúde.
    /// 
    /// DESIGN:
    /// - Struct imutável (readonly)
    /// - Sem alocação no heap
    /// - Thread-safe por imutabilidade
    /// 
    /// USO:
    /// Incluído em <see cref="LoggingHealthReport"/> para detalhar
    /// quais componentes estão causando degradação ou falha.
    /// </summary>
    public readonly struct HealthIssue
    {
        /// <summary>
        /// Nome do componente afetado.
        /// 
        /// EXEMPLOS:
        /// - "Buffer"
        /// - "Sink[ConsoleLogSink]"
        /// - "Provider"
        /// </summary>
        public string Component { get; }

        /// <summary>
        /// Severidade do problema.
        /// </summary>
        public LoggingHealthStatus Severity { get; }

        /// <summary>
        /// Descrição detalhada do problema.
        /// 
        /// EXEMPLOS:
        /// - "Buffer usage high (850/1000, 85%)"
        /// - "Sink is not operational: Connection refused"
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Timestamp de quando o problema foi detectado (UTC).
        /// </summary>
        public DateTime DetectedAtUtc { get; }

        /// <summary>
        /// Construtor completo.
        /// </summary>
        public HealthIssue(string component, LoggingHealthStatus severity, string description)
        {
            Component = component ?? throw new ArgumentNullException(nameof(component));
            Severity = severity;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            DetectedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Representação textual do issue.
        /// </summary>
        public override string ToString()
        {
            return $"[{Severity}] {Component}: {Description}";
        }
    }
}
