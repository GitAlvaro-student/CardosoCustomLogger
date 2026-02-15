using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Sinks
{
    /// <summary>
    /// Snapshot imutável de estado de um sink.
    /// 
    /// DESIGN:
    /// - Struct para evitar alocação no heap
    /// - Imutável (readonly fields)
    /// - Sem referências a objetos internos
    /// - Compatível com .NET Framework 4.6.1+
    /// 
    /// PROPÓSITO:
    /// - Representar estado de um sink sem expor implementação
    /// - Permitir diagnóstico sem vazamento de detalhes
    /// </summary>
    public readonly struct SinkHealthSnapshot
    {
        /// <summary>
        /// Nome identificador do sink.
        /// 
        /// EXEMPLOS:
        /// - "ConsoleLogSink"
        /// - "FileLogSink[C:\logs\app.log]"
        /// - "BlobStorageLogSink[mycontainer]"
        /// 
        /// USO: Identificação em relatórios de health
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Tipo do sink (nome da classe).
        /// 
        /// EXEMPLOS:
        /// - "ConsoleLogSink"
        /// - "CompositeLogSink"
        /// - "DegradableLogSink"
        /// 
        /// USO: Agrupamento por tipo em análises
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Indica se o sink está operacional.
        /// 
        /// SEMÂNTICA:
        /// - true = Sink aceitando logs
        /// - false = Sink com falha ou desabilitado
        /// 
        /// IMPORTANTE:
        /// - Definido pela implementação (ex: DegradableLogSink verifica conectividade)
        /// - Sinks sem estado sempre retornam true (ex: ConsoleLogSink)
        /// </summary>
        public bool IsOperational { get; }

        /// <summary>
        /// Informação adicional sobre estado do sink.
        /// 
        /// EXEMPLOS:
        /// - "Connected to Azure Blob Storage"
        /// - "File write failed: Access denied"
        /// - "Fallback mode active"
        /// - null ou empty = sem informação extra
        /// 
        /// USO: Diagnóstico detalhado em HealthChecks
        /// </summary>
        public string StatusMessage { get; }

        /// <summary>
        /// Timestamp da última operação bem-sucedida (UTC).
        /// 
        /// SEMÂNTICA:
        /// - null = Nunca executou com sucesso
        /// - Valor antigo = Possível problema de conectividade
        /// 
        /// USO: Detectar sinks "silenciosamente travados"
        /// </summary>
        public DateTime? LastSuccessfulWriteUtc { get; }

        /// <summary>
        /// Construtor para criar snapshot imutável.
        /// </summary>
        public SinkHealthSnapshot(
            string name,
            string type,
            bool isOperational,
            string statusMessage = null,
            DateTime? lastSuccessfulWriteUtc = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            IsOperational = isOperational;
            StatusMessage = statusMessage;
            LastSuccessfulWriteUtc = lastSuccessfulWriteUtc;
        }
    }
}

