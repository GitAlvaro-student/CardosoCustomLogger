using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.HealthChecks.Models
{
    /// <summary>
    /// Representa o status de saúde de um sink específico.
    /// 
    /// DESIGN:
    /// - Struct imutável (readonly)
    /// - Sem alocação no heap
    /// - Thread-safe por imutabilidade
    /// 
    /// USO:
    /// Usado em <see cref="LoggingHealthReport.SinkStatuses"/> para
    /// reportar saúde individual de cada sink configurado.
    /// 
    /// COMPATIBILIDADE:
    /// .NET Standard 2.0 - compatível com .NET Framework 4.6.1+ e .NET Core 2.0+
    /// </summary>
    public readonly struct SinkHealthStatus
    {
        /// <summary>
        /// Nome identificador do sink.
        /// 
        /// EXEMPLOS:
        /// - "ConsoleLogSink"
        /// - "FileLogSink[C:\logs\app.log]"
        /// - "BlobStorageLogSink[mycontainer]"
        /// 
        /// IMPORTANTE:
        /// Deve corresponder ao nome usado em <see cref="HealthIssue.Component"/>.
        /// </summary>
        public string SinkName { get; }

        /// <summary>
        /// Status de saúde deste sink específico.
        /// 
        /// SEMÂNTICA:
        /// - Unknown: Estado indeterminado
        /// - Healthy: Operando normalmente
        /// - Degraded: Operando em modo fallback/restrito
        /// - Unhealthy: Falhando ou não-operacional
        /// </summary>
        public LoggingHealthStatus Status { get; }

        /// <summary>
        /// Construtor.
        /// </summary>
        /// <param name="sinkName">Nome do sink</param>
        /// <param name="status">Status de saúde</param>
        /// <exception cref="ArgumentNullException">Se sinkName for null</exception>
        public SinkHealthStatus(string sinkName, LoggingHealthStatus status)
        {
            SinkName = sinkName ?? throw new ArgumentNullException(nameof(sinkName));
            Status = status;
        }

        /// <summary>
        /// Representação textual.
        /// </summary>
        public override string ToString()
        {
            return $"{SinkName}: {Status}";
        }
    }
}
