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
    /// - Sem alocação no heap (value type)
    /// - Thread-safe por imutabilidade
    /// - Serializável (apenas primitivos e string)
    /// 
    /// USO:
    /// Incluído em <see cref="LoggingHealthReport.Issues"/> para detalhar
    /// quais componentes estão causando degradação ou falha.
    /// 
    /// COMPATIBILIDADE:
    /// .NET Standard 2.0 - compatível com .NET Framework 4.6.1+ e .NET Core 2.0+
    /// </summary>
    public readonly struct HealthIssue
    {
        /// <summary>
        /// Nome do componente afetado.
        /// 
        /// EXEMPLOS:
        /// - "Buffer"
        /// - "Sink[ConsoleLogSink]"
        /// - "Sink[BlobStorageLogSink]"
        /// - "Provider"
        /// - "Evaluator" (quando avaliação falha)
        /// 
        /// IMPORTANTE:
        /// Não contém referência ao objeto real, apenas identificador textual.
        /// </summary>
        public string Component { get; }

        /// <summary>
        /// Severidade do problema detectado.
        /// 
        /// SEMÂNTICA:
        /// - <see cref="LoggingHealthStatus.Unknown"/>: Problema indeterminado
        /// - <see cref="LoggingHealthStatus.Degraded"/>: Funcionalidade reduzida
        /// - <see cref="LoggingHealthStatus.Unhealthy"/>: Falha crítica
        /// 
        /// IMPORTANTE:
        /// Healthy NÃO aparece em issues (sem problema = sem issue).
        /// </summary>
        public LoggingHealthStatus Severity { get; }

        /// <summary>
        /// Descrição detalhada do problema.
        /// 
        /// EXEMPLOS:
        /// - "Buffer usage high (850/1000, 85.0%)"
        /// - "Sink is not operational: Connection refused"
        /// - "Logger is operating in degraded mode"
        /// - "Buffer is discarding messages"
        /// 
        /// FORMATO:
        /// Deve ser legível por humanos e conter contexto suficiente
        /// para diagnóstico sem acesso ao estado original.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Timestamp de quando o problema foi detectado (UTC).
        /// 
        /// IMPORTANTE:
        /// - Sempre UTC para evitar problemas de timezone
        /// - Gerado no momento da criação do issue
        /// - Útil para correlação temporal em logs/métricas
        /// </summary>
        public DateTime DetectedAtUtc { get; }

        /// <summary>
        /// Construtor para criar um issue.
        /// 
        /// VALIDAÇÃO:
        /// - component e description não podem ser null
        /// - DetectedAtUtc é gerado automaticamente (UTC)
        /// </summary>
        /// <param name="component">Nome do componente afetado</param>
        /// <param name="severity">Severidade do problema</param>
        /// <param name="description">Descrição detalhada</param>
        /// <exception cref="ArgumentNullException">
        /// Se component ou description forem null
        /// </exception>
        public HealthIssue(string component, LoggingHealthStatus severity, string description)
        {
            Component = component ?? throw new ArgumentNullException(nameof(component));
            Severity = severity;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            DetectedAtUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Representação textual do issue.
        /// 
        /// FORMATO: [Severity] Component: Description
        /// EXEMPLO: [Unhealthy] Buffer: Buffer is discarding messages
        /// </summary>
        public override string ToString()
        {
            return $"[{Severity}] {Component}: {Description}";
        }
    }
}
