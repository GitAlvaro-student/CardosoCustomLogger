using CustomLogger.Abstractions;
using CustomLogger.HealthChecks.Abstractions;
using CustomLogger.HealthChecks.Models;
using CustomLogger.Sinks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CustomLogger.HealthChecks
{
    /// <summary>
    /// Implementação padrão do avaliador de saúde do logger.
    /// 
    /// REGRAS (v1.0):
    /// - Buffer &lt; 80% → Healthy
    /// - Buffer ≥ 80% → Degraded
    /// - Buffer = 100% (descartando) → Unhealthy
    /// - Sink em fallback → Degraded
    /// - Sink não-operacional → Unhealthy
    /// - Sink sem escrita há &gt; 5min → Unhealthy
    /// - Modo degradado ativo → Degraded (nunca Healthy)
    /// 
    /// AGREGAÇÃO:
    /// Pior caso vence: Unhealthy &gt; Degraded &gt; Unknown &gt; Healthy
    /// 
    /// IMPORTANTE:
    /// Leituras de estado são best-effort (snapshot pontual).
    /// Pequenas inconsistências entre propriedades são aceitáveis.
    /// </summary>
    public sealed class DefaultLoggingHealthEvaluator : ILoggingHealthEvaluator
    {
        // Thresholds fixos (v1.0)
        private const double DegradedBufferThreshold = 0.80;    // 80%
        private const double UnhealthyBufferThreshold = 1.0;    // 100%
        private static readonly TimeSpan SinkFailureWindow = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Avalia o estado de saúde do logger.
        /// 
        /// PROTEÇÃO:
        /// Try/catch envolve toda a lógica. Se algo der errado,
        /// retorna Unknown em vez de propagar exceção.
        /// </summary>
        public LoggingHealthReport Evaluate(ILoggingHealthState state)
        {
            try
            {
                // Guard: estado null
                if (state == null)
                {
                    return LoggingHealthReport.CreateUnknown("ILoggingHealthState is null");
                }

                var issues = new List<HealthIssue>();
                var sinkStatuses = new Dictionary<string, LoggingHealthStatus>();

                // Executar checks individuais
                var bufferStatus = EvaluateBuffer(state, issues);
                var sinksStatus = EvaluateSinks(state, issues, sinkStatuses);
                var degradedModeStatus = EvaluateDegradedMode(state, issues);

                // Agregar: pior caso vence
                var overallStatus = AggregateStatuses(bufferStatus, sinksStatus, degradedModeStatus);

                // Calcular percentual de uso do buffer
                var bufferUsagePercentage = CalculateBufferUsagePercentage(state);

                return new LoggingHealthReport(
                    status: overallStatus,
                    issues: issues.AsReadOnly(),
                    sinkStatuses: sinkStatuses,
                    bufferUsagePercentage: bufferUsagePercentage,
                    evaluatedAtUtc: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                // Fallback defensivo: nunca propagar exceção
                return LoggingHealthReport.CreateUnknown(
                    $"Health evaluation failed: {ex.GetType().Name} - {ex.Message}"
                );
            }
        }

        #region Buffer Check

        /// <summary>
        /// CHECK 1: Avalia estado do buffer.
        /// 
        /// REGRAS:
        /// - Descartando mensagens → Unhealthy
        /// - Buffer ≥ 100% → Unhealthy
        /// - Buffer ≥ 80% → Degraded
        /// - Buffer &lt; 80% → Healthy
        /// 
        /// PROTEÇÃO:
        /// - Capacidade &lt;= 0 → Unknown
        /// - Tamanho &lt; 0 → Unknown
        /// </summary>
        private LoggingHealthStatus EvaluateBuffer(ILoggingHealthState state, List<HealthIssue> issues)
        {
            try
            {
                // Validação: capacidade inválida
                if (state.MaxBufferCapacity <= 0)
                {
                    issues.Add(new HealthIssue(
                        "Buffer",
                        LoggingHealthStatus.Unknown,
                        "Invalid buffer configuration (MaxBufferCapacity <= 0)"
                    ));
                    return LoggingHealthStatus.Unknown;
                }

                // Validação: tamanho negativo
                if (state.CurrentBufferSize < 0)
                {
                    issues.Add(new HealthIssue(
                        "Buffer",
                        LoggingHealthStatus.Unknown,
                        "Invalid buffer state (CurrentBufferSize < 0)"
                    ));
                    return LoggingHealthStatus.Unknown;
                }

                // Calcular percentual de ocupação
                double fillPercentage = (double)state.CurrentBufferSize / state.MaxBufferCapacity;

                // REGRA 1: Buffer descartando mensagens (crítico)
                if (state.IsDiscardingMessages)
                {
                    issues.Add(new HealthIssue(
                        "Buffer",
                        LoggingHealthStatus.Unhealthy,
                        $"Buffer is discarding messages (size: {state.CurrentBufferSize}/{state.MaxBufferCapacity}, {fillPercentage:P1})"
                    ));
                    return LoggingHealthStatus.Unhealthy;
                }

                // REGRA 2: Buffer em capacidade máxima (crítico)
                if (fillPercentage >= UnhealthyBufferThreshold)
                {
                    issues.Add(new HealthIssue(
                        "Buffer",
                        LoggingHealthStatus.Unhealthy,
                        $"Buffer is full ({state.CurrentBufferSize}/{state.MaxBufferCapacity}, {fillPercentage:P1})"
                    ));
                    return LoggingHealthStatus.Unhealthy;
                }

                // REGRA 3: Buffer acima de threshold degradado
                if (fillPercentage >= DegradedBufferThreshold)
                {
                    issues.Add(new HealthIssue(
                        "Buffer",
                        LoggingHealthStatus.Degraded,
                        $"Buffer usage high ({state.CurrentBufferSize}/{state.MaxBufferCapacity}, {fillPercentage:P1})"
                    ));
                    return LoggingHealthStatus.Degraded;
                }

                // Buffer saudável (< 80%)
                return LoggingHealthStatus.Healthy;
            }
            catch (Exception ex)
            {
                // Proteção: avaliação do buffer falhou
                issues.Add(new HealthIssue(
                    "Buffer",
                    LoggingHealthStatus.Unknown,
                    $"Buffer evaluation failed: {ex.Message}"
                ));
                return LoggingHealthStatus.Unknown;
            }
        }

        #endregion

        #region Sinks Check

        /// <summary>
        /// CHECK 2: Avalia estado de todos os sinks.
        /// 
        /// REGRAS:
        /// - Sink não-operacional → Unhealthy
        /// - Sink sem escrita há &gt; 5min → Unhealthy
        /// - Sink em fallback/degraded → Degraded
        /// - Todos sinks saudáveis → Healthy
        /// 
        /// AGREGAÇÃO:
        /// Pior status entre todos os sinks vence.
        /// 
        /// EDGE CASE:
        /// - Nenhum sink configurado → Degraded (configuração inválida)
        /// </summary>
        private LoggingHealthStatus EvaluateSinks(
            ILoggingHealthState state,
            List<HealthIssue> issues,
            Dictionary<string, LoggingHealthStatus> sinkStatuses)
        {
            try
            {
                var sinkStates = state.SinkStates;

                // Validação: lista de sinks null
                if (sinkStates == null)
                {
                    issues.Add(new HealthIssue(
                        "Sinks",
                        LoggingHealthStatus.Unknown,
                        "SinkStates is null"
                    ));
                    return LoggingHealthStatus.Unknown;
                }

                // Edge case: nenhum sink configurado
                if (sinkStates.Count == 0)
                {
                    issues.Add(new HealthIssue(
                        "Sinks",
                        LoggingHealthStatus.Degraded,
                        "No sinks configured"
                    ));
                    return LoggingHealthStatus.Degraded;
                }

                var worstSinkStatus = LoggingHealthStatus.Healthy;
                var now = DateTime.UtcNow;

                // Avaliar cada sink individualmente
                foreach (var sink in sinkStates)
                {
                    var sinkStatus = EvaluateSingleSink(sink, now, issues);
                    sinkStatuses[sink.Name] = sinkStatus;

                    // Agregar: pior vence
                    if (sinkStatus > worstSinkStatus)
                    {
                        worstSinkStatus = sinkStatus;
                    }
                }

                return worstSinkStatus;
            }
            catch (Exception ex)
            {
                // Proteção: avaliação de sinks falhou
                issues.Add(new HealthIssue(
                    "Sinks",
                    LoggingHealthStatus.Unknown,
                    $"Sinks evaluation failed: {ex.Message}"
                ));
                return LoggingHealthStatus.Unknown;
            }
        }

        /// <summary>
        /// Avalia um único sink.
        /// 
        /// REGRAS:
        /// 1. IsOperational = false → Unhealthy
        /// 2. LastSuccessfulWrite &gt; 5min atrás → Unhealthy
        /// 3. StatusMessage contém "fallback" ou "degraded" → Degraded
        /// 4. Caso contrário → Healthy
        /// </summary>
        private LoggingHealthStatus EvaluateSingleSink(
            SinkHealthSnapshot sink,
            DateTime now,
            List<HealthIssue> issues)
        {
            var componentName = $"Sink[{sink.Name}]";

            // REGRA 1: Sink reporta não-operacional
            if (!sink.IsOperational)
            {
                issues.Add(new HealthIssue(
                    componentName,
                    LoggingHealthStatus.Unhealthy,
                    $"{sink.Type} is not operational: {sink.StatusMessage ?? "Unknown reason"}"
                ));
                return LoggingHealthStatus.Unhealthy;
            }

            // REGRA 2: Sink sem escrita recente (possível falha silenciosa)
            if (sink.LastSuccessfulWriteUtc.HasValue)
            {
                var timeSinceLastSuccess = now - sink.LastSuccessfulWriteUtc.Value;

                if (timeSinceLastSuccess > SinkFailureWindow)
                {
                    issues.Add(new HealthIssue(
                        componentName,
                        LoggingHealthStatus.Unhealthy,
                        $"{sink.Type} has not written successfully in {timeSinceLastSuccess.TotalMinutes:F1} minutes"
                    ));
                    return LoggingHealthStatus.Unhealthy;
                }
            }

            // REGRA 3: Sink em modo fallback ou degradado (inferido por StatusMessage)
            if (!string.IsNullOrEmpty(sink.StatusMessage))
            {
                var message = sink.StatusMessage.ToLowerInvariant();

                if (message.Contains("fallback") || message.Contains("degraded"))
                {
                    issues.Add(new HealthIssue(
                        componentName,
                        LoggingHealthStatus.Degraded,
                        $"{sink.Type} status: {sink.StatusMessage}"
                    ));
                    return LoggingHealthStatus.Degraded;
                }
            }

            // Sink saudável
            return LoggingHealthStatus.Healthy;
        }

        #endregion

        #region Degraded Mode Check

        /// <summary>
        /// CHECK 3: Avalia se modo degradado está ativo.
        /// 
        /// REGRA:
        /// - Modo degradado ativo → Degraded (NUNCA Healthy)
        /// - Modo degradado inativo → Healthy
        /// 
        /// IMPORTANTE:
        /// Quando modo degradado está ativo, o status global NUNCA pode ser Healthy,
        /// mesmo que buffer e sinks estejam bem.
        /// </summary>
        private LoggingHealthStatus EvaluateDegradedMode(ILoggingHealthState state, List<HealthIssue> issues)
        {
            try
            {
                if (state.IsDegradedMode)
                {
                    issues.Add(new HealthIssue(
                        "Provider",
                        LoggingHealthStatus.Degraded,
                        "Logger is operating in degraded mode"
                    ));
                    return LoggingHealthStatus.Degraded;
                }

                return LoggingHealthStatus.Healthy;
            }
            catch (Exception ex)
            {
                // Proteção: avaliação de modo degradado falhou
                issues.Add(new HealthIssue(
                    "Provider",
                    LoggingHealthStatus.Unknown,
                    $"Degraded mode evaluation failed: {ex.Message}"
                ));
                return LoggingHealthStatus.Unknown;
            }
        }

        #endregion

        #region Aggregation

        /// <summary>
        /// Agrega múltiplos status em um único resultado.
        /// 
        /// REGRA: Pior caso vence
        /// 1. Se qualquer = Unhealthy → Unhealthy
        /// 2. Senão, se qualquer = Degraded → Degraded
        /// 3. Senão, se qualquer = Unknown → Unknown
        /// 4. Senão → Healthy
        /// 
        /// IMPORTANTE:
        /// Esta ordem NÃO deve ser alterada.
        /// </summary>
        private LoggingHealthStatus AggregateStatuses(params LoggingHealthStatus[] statuses)
        {
            // Prioridade 1: Unhealthy
            if (statuses.Any(s => s == LoggingHealthStatus.Unhealthy))
                return LoggingHealthStatus.Unhealthy;

            // Prioridade 2: Degraded
            if (statuses.Any(s => s == LoggingHealthStatus.Degraded))
                return LoggingHealthStatus.Degraded;

            // Prioridade 3: Unknown
            if (statuses.Any(s => s == LoggingHealthStatus.Unknown))
                return LoggingHealthStatus.Unknown;

            // Default: Healthy
            return LoggingHealthStatus.Healthy;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calcula percentual de uso do buffer (0.0 a 100.0).
        /// 
        /// RETORNO:
        /// - 0.0 a 100.0: Percentual válido
        /// - -1.0: Informação indisponível (erro de cálculo)
        /// 
        /// PROTEÇÃO:
        /// Não lança exceção, retorna -1.0 em caso de erro.
        /// </summary>
        private double CalculateBufferUsagePercentage(ILoggingHealthState state)
        {
            try
            {
                if (state.MaxBufferCapacity <= 0)
                    return -1.0;

                if (state.CurrentBufferSize < 0)
                    return -1.0;

                return ((double)state.CurrentBufferSize / state.MaxBufferCapacity) * 100.0;
            }
            catch
            {
                return -1.0;
            }
        }

        #endregion
    }
}
