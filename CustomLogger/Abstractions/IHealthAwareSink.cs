using CustomLogger.Sinks;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Abstractions
{
    /// <summary>
    /// Interface OPCIONAL para sinks que podem reportar estado de saúde.
    /// 
    /// IMPORTANTE:
    /// - Implementação é OPCIONAL
    /// - Sinks simples (Console, File) podem ignorar
    /// - Sinks complexos (BlobStorage, Dynatrace) devem implementar
    /// </summary>
    public interface IHealthAwareSink
    {
        /// <summary>
        /// Retorna snapshot de estado atual do sink.
        /// 
        /// CONTRATO:
        /// - NUNCA lança exceção
        /// - NUNCA escreve logs
        /// - Execução rápida (< 10ms)
        /// </summary>
        SinkHealthSnapshot GetHealthSnapshot();
    }
}
