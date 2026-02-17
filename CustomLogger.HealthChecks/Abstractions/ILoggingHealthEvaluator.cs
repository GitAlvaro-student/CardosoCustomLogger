using CustomLogger.Abstractions;
using CustomLogger.HealthChecks.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.HealthChecks.Abstractions
{
    /// <summary>
    /// Avalia o estado de saúde do sistema de logging.
    /// 
    /// CONTRATO:
    /// - Função pura: mesma entrada sempre produz mesma saída
    /// - NUNCA lança exceção (retorna Unknown em caso de falha)
    /// - NUNCA retorna null
    /// - Thread-safe (stateless)
    /// </summary>
    public interface ILoggingHealthEvaluator
    {
        /// <summary>
        /// Avalia o estado de saúde do logger e gera relatório.
        /// </summary>
        /// <param name="state">Estado atual do logger. Se null, retorna Unknown.</param>
        /// <returns>Relatório de saúde. NUNCA null.</returns>
        LoggingHealthReport Evaluate(ILoggingHealthState state);
    }
}
