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
    /// RESPONSABILIDADE ÚNICA:
    /// - Receber snapshot de estado (<see cref="ILoggingHealthState"/>)
    /// - Aplicar regras de avaliação
    /// - Retornar relatório de saúde (<see cref="LoggingHealthReport"/>)
    /// 
    /// CONTRATO:
    /// - Função PURA: mesma entrada sempre produz mesma saída
    /// - NUNCA lança exceção (retorna Unknown em caso de falha)
    /// - NUNCA retorna null
    /// - NUNCA altera estado do logger
    /// - NUNCA escreve logs
    /// - Thread-safe (stateless)
    /// 
    /// IMPORTANTE:
    /// Este componente é OBSERVADOR passivo. Não interfere no funcionamento
    /// do logger, apenas lê estado e emite opinião sobre saúde.
    /// 
    /// COMPATIBILIDADE:
    /// .NET Standard 2.0 - compatível com .NET Framework 4.6.1+ e .NET Core 2.0+
    /// </summary>
    public interface ILoggingHealthEvaluator
    {
        /// <summary>
        /// Avalia o estado de saúde do logger e gera relatório.
        /// 
        /// GARANTIAS:
        /// - NUNCA lança exceção (proteção try/catch interna)
        /// - NUNCA retorna null (sempre retorna relatório válido)
        /// - Execução rápida (target &lt; 100ms)
        /// - Stateless (não mantém cache entre chamadas)
        /// 
        /// COMPORTAMENTO EM CASO DE FALHA:
        /// Se avaliação falhar (exceção, estado inválido, etc.):
        /// - Retorna relatório com status <see cref="LoggingHealthStatus.Unknown"/>
        /// - Inclui mensagem de erro no relatório
        /// - NÃO propaga exceção
        /// 
        /// REGRA DE AGREGAÇÃO:
        /// Quando múltiplas condições são avaliadas:
        /// 1. Se qualquer = Unhealthy → resultado = Unhealthy
        /// 2. Senão, se qualquer = Degraded → resultado = Degraded
        /// 3. Senão, se qualquer = Unknown → resultado = Unknown
        /// 4. Senão → resultado = Healthy
        /// 
        /// THREAD-SAFETY:
        /// Método thread-safe por ser stateless. Múltiplas threads podem
        /// chamar simultaneamente sem problemas.
        /// </summary>
        /// <param name="state">
        /// Estado atual do logger. Se null, retorna relatório Unknown.
        /// </param>
        /// <returns>
        /// Relatório de saúde. NUNCA null.
        /// </returns>
        LoggingHealthReport Evaluate(ILoggingHealthState state);
    }
}
