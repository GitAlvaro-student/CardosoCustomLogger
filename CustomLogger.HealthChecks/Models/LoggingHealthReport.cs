using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomLogger.HealthChecks.Models
{
    /// <summary>
    /// Relatório de saúde do sistema de logging.
    /// 
    /// PROPÓSITO:
    /// Snapshot imutável do estado de saúde do logger em um momento específico.
    /// Contém status global, problemas detectados, estado dos sinks e métricas do buffer.
    /// 
    /// DESIGN:
    /// - Class (não struct) para evitar cópias grandes por valor
    /// - Imutável: todas propriedades readonly, coleções IReadOnly*
    /// - Thread-safe por imutabilidade
    /// - Serializável: apenas tipos primitivos e coleções básicas
    /// 
    /// IMPORTANTE:
    /// Este relatório NÃO mantém referências a objetos internos do logger.
    /// É um snapshot independente, seguro para armazenamento e transmissão.
    /// 
    /// COMPATIBILIDADE:
    /// .NET Standard 2.0 - compatível com .NET Framework 4.6.1+ e .NET Core 2.0+
    /// </summary>
    public sealed class LoggingHealthReport
    {
        /// <summary>
        /// Status global de saúde do sistema de logging.
        /// 
        /// AGREGAÇÃO:
        /// Representa o pior status entre todos os componentes avaliados.
        /// - Unknown: Se avaliação falhou ou dados insuficientes
        /// - Healthy: Todos componentes operacionais
        /// - Degraded: Um ou mais componentes com funcionalidade reduzida
        /// - Unhealthy: Um ou mais componentes falhando criticamente
        /// 
        /// IMPORTANTE:
        /// Este é o status que deve ser usado para decisões de alto nível
        /// (ex: readiness probe em Kubernetes).
        /// </summary>
        public LoggingHealthStatus Status { get; }

        /// <summary>
        /// Lista estruturada de problemas detectados.
        /// 
        /// SEMÂNTICA:
        /// - Vazia se Status = Healthy
        /// - Contém 1+ items se Status = Degraded, Unhealthy ou Unknown
        /// 
        /// ORDENAÇÃO:
        /// Não garantida. Consumidores devem ordenar por Severity se necessário.
        /// 
        /// IMUTABILIDADE:
        /// IReadOnlyList impede modificação externa.
        /// </summary>
        public IReadOnlyList<HealthIssue> Issues { get; }

        /// <summary>
        /// Status de saúde individual de cada sink.
        /// 
        /// SEMÂNTICA:
        /// - Chave: Nome do sink (ex: "ConsoleLogSink", "FileLogSink[path]")
        /// - Valor: Status de saúde daquele sink específico
        /// 
        /// USO:
        /// Permite identificar qual sink específico está causando problemas.
        /// 
        /// VAZIA:
        /// Pode estar vazia se nenhum sink configurado (configuração inválida)
        /// ou se avaliação de sinks falhou.
        /// 
        /// IMUTABILIDADE:
        /// IReadOnlyDictionary impede adição/remoção de sinks.
        /// </summary>
        public IReadOnlyDictionary<string, LoggingHealthStatus> SinkStatuses { get; }

        /// <summary>
        /// Percentual de uso do buffer (0.0 a 100.0).
        /// 
        /// CÁLCULO:
        /// (CurrentBufferSize / MaxBufferCapacity) * 100
        /// 
        /// EXEMPLOS:
        /// - 0.0 = Buffer vazio
        /// - 50.0 = Buffer pela metade
        /// - 85.0 = Buffer no threshold de Degraded
        /// - 100.0 = Buffer cheio (Unhealthy)
        /// 
        /// VALOR ESPECIAL:
        /// - -1.0 = Informação indisponível (ex: avaliação falhou)
        /// 
        /// TIPO: double (não decimal)
        /// JUSTIFICATIVA:
        /// - Percentual é valor aproximado (não requer precisão exata)
        /// - double é mais performático que decimal
        /// - Compatível com JSON/serialização padrão
        /// - Padrão da indústria para métricas (Prometheus, Grafana)
        /// </summary>
        public double BufferUsagePercentage { get; }

        /// <summary>
        /// Timestamp de quando o relatório foi gerado (UTC).
        /// 
        /// IMPORTANTE:
        /// - Sempre UTC para evitar problemas de timezone
        /// - Útil para correlação temporal
        /// - Permite detectar relatórios obsoletos
        /// </summary>
        public DateTime EvaluatedAtUtc { get; }

        /// <summary>
        /// Construtor principal.
        /// 
        /// VALIDAÇÃO:
        /// - issues não pode ser null (usar lista vazia se sem problemas)
        /// - sinkStatuses não pode ser null (usar dicionário vazio se sem sinks)
        /// - bufferUsagePercentage deve estar entre -1.0 e 100.0
        /// </summary>
        /// <param name="status">Status global de saúde</param>
        /// <param name="issues">Lista de problemas detectados</param>
        /// <param name="sinkStatuses">Status individual de cada sink</param>
        /// <param name="bufferUsagePercentage">Percentual de uso do buffer (0-100 ou -1 se indisponível)</param>
        /// <param name="evaluatedAtUtc">Timestamp da avaliação (UTC)</param>
        /// <exception cref="ArgumentNullException">Se issues ou sinkStatuses forem null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se bufferUsagePercentage inválido</exception>
        public LoggingHealthReport(
            LoggingHealthStatus status,
            IReadOnlyList<HealthIssue> issues,
            IReadOnlyDictionary<string, LoggingHealthStatus> sinkStatuses,
            double bufferUsagePercentage,
            DateTime evaluatedAtUtc)
        {
            Status = status;
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            SinkStatuses = sinkStatuses ?? throw new ArgumentNullException(nameof(sinkStatuses));

            // Validar percentual
            if (bufferUsagePercentage < -1.0 || bufferUsagePercentage > 100.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bufferUsagePercentage),
                    bufferUsagePercentage,
                    "Buffer usage percentage must be between -1.0 and 100.0"
                );
            }

            BufferUsagePercentage = bufferUsagePercentage;
            EvaluatedAtUtc = evaluatedAtUtc;
        }

        /// <summary>
        /// Factory method: Cria relatório Healthy padrão.
        /// 
        /// USO:
        /// Quando todos os checks passam sem problemas.
        /// </summary>
        /// <param name="sinkStatuses">Status dos sinks (todos Healthy)</param>
        /// <param name="bufferUsagePercentage">Percentual atual do buffer</param>
        /// <returns>Relatório com status Healthy</returns>
        public static LoggingHealthReport CreateHealthy(
            IReadOnlyDictionary<string, LoggingHealthStatus> sinkStatuses,
            double bufferUsagePercentage)
        {
            return new LoggingHealthReport(
                status: LoggingHealthStatus.Healthy,
                issues: Array.Empty<HealthIssue>(),
                sinkStatuses: sinkStatuses ?? new Dictionary<string, LoggingHealthStatus>(),
                bufferUsagePercentage: bufferUsagePercentage,
                evaluatedAtUtc: DateTime.UtcNow
            );
        }

        /// <summary>
        /// Factory method: Cria relatório Unknown (usado quando avaliação falha).
        /// 
        /// USO:
        /// Quando avaliador encontra exceção ou estado inconsistente.
        /// </summary>
        /// <param name="errorMessage">Descrição do erro</param>
        /// <returns>Relatório com status Unknown</returns>
        public static LoggingHealthReport CreateUnknown(string errorMessage)
        {
            var issue = new HealthIssue(
                component: "Evaluator",
                severity: LoggingHealthStatus.Unknown,
                description: errorMessage ?? "Unknown error during evaluation"
            );

            return new LoggingHealthReport(
                status: LoggingHealthStatus.Unknown,
                issues: new[] { issue },
                sinkStatuses: new Dictionary<string, LoggingHealthStatus>(),
                bufferUsagePercentage: -1.0, // Indisponível
                evaluatedAtUtc: DateTime.UtcNow
            );
        }

        /// <summary>
        /// Representação textual do relatório.
        /// 
        /// FORMATO:
        /// [Status] Buffer: X.X%, Sinks: Y/Z healthy, Issues: N
        /// </summary>
        public override string ToString()
        {
            var healthySinks = SinkStatuses.Count(s => s.Value == LoggingHealthStatus.Healthy);
            var totalSinks = SinkStatuses.Count;

            var bufferInfo = BufferUsagePercentage >= 0
                ? $"{BufferUsagePercentage:F1}%"
                : "N/A";

            return $"[{Status}] Buffer: {bufferInfo}, Sinks: {healthySinks}/{totalSinks} healthy, Issues: {Issues.Count}";
        }
    }
}
