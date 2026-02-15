using CustomLogger.Sinks;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    /// <summary>
    /// Contrato para leitura de estado interno do sistema de logging.
    /// 
    /// PROPÓSITO:
    /// - Permite observação passiva do estado do logger
    /// - NÃO permite modificação de estado
    /// - NÃO escreve logs durante leitura
    /// - Thread-safe para leitura concorrente
    /// 
    /// IMPORTANTE:
    /// - Implementado por CustomLoggerProvider
    /// - Consumido por CustomLogger.HealthChecks
    /// - Snapshot pontual (não mantém referências mutáveis)
    /// </summary>
    public interface ILoggingHealthState
    {
        /// <summary>
        /// Número atual de logs pendentes no buffer.
        /// 
        /// SEMÂNTICA:
        /// - 0 = buffer vazio (saudável)
        /// - Próximo de MaxBufferCapacity = possível problema
        /// - Valor estável = consumo equilibrado
        /// - Crescimento constante = sinks lentos
        /// 
        /// THREAD-SAFETY: Snapshot atômico no momento da leitura
        /// </summary>
        int CurrentBufferSize { get; }

        /// <summary>
        /// Capacidade máxima configurada do buffer.
        /// 
        /// SEMÂNTICA:
        /// - Define limite de enfileiramento
        /// - Quando atingido, logs são descartados
        /// - Valor fixo (configuração)
        /// 
        /// USO: Calcular percentual de ocupação (CurrentBufferSize / MaxBufferCapacity)
        /// </summary>
        int MaxBufferCapacity { get; }

        /// <summary>
        /// Indica se logs estão sendo descartados por buffer cheio.
        /// 
        /// SEMÂNTICA:
        /// - true = Sistema sob pressão, perdendo logs
        /// - false = Buffer operando dentro da capacidade
        /// 
        /// IMPORTANTE:
        /// - Flag baseada em estado atual, não histórico
        /// - HealthCheck deve interpretar: true = Unhealthy
        /// </summary>
        bool IsDiscardingMessages { get; }

        /// <summary>
        /// Indica se o sistema está operando em modo degradado.
        /// 
        /// SEMÂNTICA (conforme ProviderLifeCycle):
        /// - true = Provider em estado DEGRADED
        /// - false = Provider em estado OPERATIONAL ou outros
        /// 
        /// CONTEXTO:
        /// - Modo degradado = fallback ativo, funcionalidade reduzida
        /// - HealthCheck deve interpretar: true = Degraded
        /// </summary>
        bool IsDegradedMode { get; }

        /// <summary>
        /// Snapshot de estado dos sinks configurados.
        /// 
        /// SEMÂNTICA:
        /// - Coleção imutável (IReadOnlyList)
        /// - Cada item representa um sink individual
        /// - Lista vazia = sem sinks (configuração inválida)
        /// 
        /// IMPORTANTE:
        /// - NÃO expõe instâncias ILogSink concretas
        /// - Apenas metadados de estado
        /// </summary>
        IReadOnlyList<SinkHealthSnapshot> SinkStates { get; }
    }
}
