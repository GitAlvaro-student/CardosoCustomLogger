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
    /// DESIGN:
    /// - Stateless (sem campos mutáveis)
    /// - Thread-safe por design
    /// - Thresholds fixos (não configuráveis nesta versão)
    /// - Proteção defensiva contra exceções
    /// 
    /// REGRAS DE AVALIAÇÃO (v1.0):
    /// - Buffer &lt; 80% → Healthy
    /// - Buffer ≥ 80% → Degraded
    /// - Buffer = 100% (descartando) → Unhealthy
    /// - Sink em fallback → Degraded
    /// - Sink falhando (LastSuccessfulWrite &gt; 5min) → Unhealthy
    /// - Modo degradado ativo → Degraded
    /// 
    /// FUTURO:
    /// Thresholds configuráveis via options pattern (sem quebrar compatibilidade).
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
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // GUARD: Estado null
                if (state == null)
                {
                    return LoggingHealthReport.Unknown(
                        "ILoggingHealthState is null",
                        stopwatch.Elapsed.TotalMilliseconds
                    );
                }

                // Coletar issues de todos os checks
                var issues = new List<HealthIssue>();

                // Check 1: Buffer
                var bufferStatus = EvaluateBuffer(state, issues);

                // Check 2: Sinks
                var sinksStatus = EvaluateSinks(state, issues);

                // Check 3: Modo Degradado
                var degradedModeStatus = EvaluateDegradedMode(state, issues);

                // Agregação: pior caso vence
                var overallStatus = AggregateStatuses(bufferStatus, sinksStatus, degradedModeStatus);

                stopwatch.Stop();

                return new LoggingHealthReport(
                    status: overallStatus,
                    issues: issues.AsReadOnly(),
                    evaluationDurationMs: stopwatch.Elapsed.TotalMilliseconds,
                    evaluatedAtUtc: DateTime.UtcNow
                );
            }
            catch (Exception ex)
            {
                // Fallback defensivo: nunca propagar exceção
                stopwatch.Stop();

                return LoggingHealthReport.Unknown(
                    $"Health evaluation failed: {ex.GetType().Name} - {ex.Message}",
                    stopwatch.Elapsed.TotalMilliseconds
                );
            }
        }

        /// <summary>
        /// Avalia estado do buffer.
        /// 
        /// REGRAS:
        /// - Descartando mensagens → Unhealthy
        /// - Buffer ≥ 80% → Degraded
        /// - Buffer &lt; 80% → Healthy
        /// </summary>
        private LoggingHealthStatus EvaluateBuffer(ILoggingHealthState state, List<HealthIssue> issues)
        {
            try
            {
                // Proteção: capacidade zero (configuração inválida)
                if (state.MaxBufferCapacity <= 0)
                {
                    issues.Add(new HealthIssue(
                        component: "Buffer",
                        severity: LoggingHealthStatus.Unknown,
                        description: "Invalid buffer configuration (MaxBufferCapacity <= 0)"
                    ));
                    return LoggingHealthStatus.Unknown;
                }

                // Proteção: tamanho negativo (estado inconsistente)
                if (state.CurrentBufferSize < 0)
                {
                    issues.Add(new HealthIssue(
                        component: "Buffer",
                        severity: LoggingHealthStatus.Unknown,
                        description: "Invalid buffer state (CurrentBufferSize < 0)"
                    ));
                    return LoggingHealthStatus.Unknown;
                }

                // Calcular percentual de ocupação
                double fillPercentage = (double)state.CurrentBufferSize / state.MaxBufferCapacity;

                // REGRA 1: Buffer descartando mensagens
                if (state.IsDiscardingMessages)
                {
                    issues.Add(new HealthIssue(
                        component: "Buffer",
                        severity: LoggingHealthStatus.Unhealthy,
                        description: $"Buffer is discarding messages (size: {state.CurrentBufferSize}/{state.MaxBufferCapacity}, {fillPercentage:P1})"
                    ));
                    return LoggingHealthStatus.Unhealthy;
                }

                // REGRA 2: Buffer em capacidade máxima (mesmo sem descartar ainda)
                if (fillPercentage >= UnhealthyBufferThreshold)
                {
                    issues.Add(new HealthIssue(
                        component: "Buffer",
                        severity: LoggingHealthStatus.Unhealthy,
                        description: $"Buffer is full ({state.CurrentBufferSize}/{state.MaxBufferCapacity}, {fillPercentage:P1})"
                    ));
                    return LoggingHealthStatus.Unhealthy;
                }

                // REGRA 3: Buffer acima de threshold degraded
                if (fillPercentage >= DegradedBufferThreshold)
                {
                    issues.Add(new HealthIssue(
                        component: "Buffer",
                        severity: LoggingHealthStatus.Degraded,
                        description: $"Buffer usage high ({state.CurrentBufferSize}/{state.MaxBufferCapacity}, {fillPercentage:P1})"
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
                    component: "Buffer",
                    severity: LoggingHealthStatus.Unknown,
                    description: $"Buffer evaluation failed: {ex.Message}"
                ));
                return LoggingHealthStatus.Unknown;
            }
        }

        /// <summary>
        /// Avalia estado de todos os sinks.
        /// 
        /// REGRAS:
        /// - Sink não-operacional → Unhealthy
        /// - Sink sem escrita há &gt; 5min → Unhealthy
        /// - Sink reportando fallback → Degraded
        /// - Todos sinks saudáveis → Healthy
        /// 
        /// AGREGAÇÃO:
        /// Pior status entre todos os sinks vence.
        /// </summary>
        private LoggingHealthStatus EvaluateSinks(ILoggingHealthState state, List<HealthIssue> issues)
        {
            try
            {
                var sinkStates = state.SinkStates;

                // Proteção: lista de sinks null
                if (sinkStates == null)
                {
                    issues.Add(new HealthIssue(
                        component: "Sinks",
                        severity: LoggingHealthStatus.Unknown,
                        description: "SinkStates is null"
                    ));
                    return LoggingHealthStatus.Unknown;
                }

                // Caso edge: nenhum sink configurado
                if (sinkStates.Count == 0)
                {
                    issues.Add(new HealthIssue(
                        component: "Sinks",
                        severity: LoggingHealthStatus.Degraded,
                        description: "No sinks configured"
                    ));
                    return LoggingHealthStatus.Degraded;
                }

                var worstSinkStatus = LoggingHealthStatus.Healthy;
                var now = DateTime.UtcNow;

                foreach (var sink in sinkStates)
                {
                    var sinkStatus = EvaluateSingleSink(sink, now, issues);

                    // Agregar: pior status vence
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
                    component: "Sinks",
                    severity: LoggingHealthStatus.Unknown,
                    description: $"Sinks evaluation failed: {ex.Message}"
                ));
                return LoggingHealthStatus.Unknown;
            }
        }

        /// <summary>
        /// Avalia um único sink.
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
                    component: componentName,
                    severity: LoggingHealthStatus.Unhealthy,
                    description: $"{sink.Type} is not operational: {sink.StatusMessage ?? "Unknown reason"}"
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
                        component: componentName,
                        severity: LoggingHealthStatus.Unhealthy,
                        description: $"{sink.Type} has not written successfully in {timeSinceLastSuccess.TotalMinutes:F1} minutes"
                    ));
                    return LoggingHealthStatus.Unhealthy;
                }
            }

            // REGRA 3: Sink em modo fallback (inferido por StatusMessage)
            if (!string.IsNullOrEmpty(sink.StatusMessage))
            {
                var message = sink.StatusMessage.ToLowerInvariant();

                if (message.Contains("fallback") || message.Contains("degraded"))
                {
                    issues.Add(new HealthIssue(
                        component: componentName,
                        severity: LoggingHealthStatus.Degraded,
                        description: $"{sink.Type} status: {sink.StatusMessage}"
                    ));
                    return LoggingHealthStatus.Degraded;
                }
            }

            // Sink saudável
            return LoggingHealthStatus.Healthy;
        }

        /// <summary>
        /// Avalia se modo degradado está ativo.
        /// 
        /// REGRA:
        /// - Modo degradado ativo → Degraded (nunca Healthy)
        /// - Modo degradado inativo → Healthy
        /// </summary>
        private LoggingHealthStatus EvaluateDegradedMode(ILoggingHealthState state, List<HealthIssue> issues)
        {
            try
            {
                if (state.IsDegradedMode)
                {
                    issues.Add(new HealthIssue(
                        component: "Provider",
                        severity: LoggingHealthStatus.Degraded,
                        description: "Logger is operating in degraded mode"
                    ));
                    return LoggingHealthStatus.Degraded;
                }

                return LoggingHealthStatus.Healthy;
            }
            catch (Exception ex)
            {
                // Proteção: avaliação de modo degradado falhou
                issues.Add(new HealthIssue(
                    component: "Provider",
                    severity: LoggingHealthStatus.Unknown,
                    description: $"Degraded mode evaluation failed: {ex.Message}"
                ));
                return LoggingHealthStatus.Unknown;
            }
        }

        /// <summary>
        /// Agrega múltiplos status em um único resultado.
        /// 
        /// REGRA: Pior caso vence
        /// 1. Se qualquer = Unhealthy → Unhealthy
        /// 2. Senão, se qualquer = Degraded → Degraded
        /// 3. Senão, se qualquer = Unknown → Unknown
        /// 4. Senão → Healthy
        /// </summary>
        private LoggingHealthStatus AggregateStatuses(params LoggingHealthStatus[] statuses)
        {
            // Usar Max() para pegar o maior valor (pior status)
            // Ordem: Unknown(0) < Healthy(1) < Degraded(2) < Unhealthy(3)

            // Porém Unknown é especial: deve ter prioridade apenas se não houver pior
            var hasUnhealthy = statuses.Any(s => s == LoggingHealthStatus.Unhealthy);
            if (hasUnhealthy) return LoggingHealthStatus.Unhealthy;

            var hasDegraded = statuses.Any(s => s == LoggingHealthStatus.Degraded);
            if (hasDegraded) return LoggingHealthStatus.Degraded;

            var hasUnknown = statuses.Any(s => s == LoggingHealthStatus.Unknown);
            if (hasUnknown) return LoggingHealthStatus.Unknown;

            return LoggingHealthStatus.Healthy;
        }
    }
}
