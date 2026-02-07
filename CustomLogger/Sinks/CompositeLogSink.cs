using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    /// <summary>
    /// Sink composto que orquestra múltiplos sinks com fallback sequencial.
    /// 
    /// RFC - FALLBACK ENTRE SINKS:
    /// - Fallback NÃO é mecanismo especial - é apenas "tentar próximo sink"
    /// - Ordem de sinks é fixa (definida no builder)
    /// - Cada sink tentado NO MÁXIMO UMA VEZ por flush
    /// - Falha de um sink NÃO impede próximos
    /// - NENHUMA exceção pode escapar
    /// - SEM loops infinitos
    /// - SEM violação de shutdown
    /// </summary>
    public sealed class CompositeLogSink : ILogSink, IBatchLogSink, IAsyncLogSink, IAsyncBatchLogSink
    {
        // RFC: Ordem de sinks é fixa e imutável após construção
        private readonly IReadOnlyList<ILogSink> _sinks;

        /// <summary>
        /// Cria sink composto com lista fixa de sinks.
        /// </summary>
        /// <param name="sinks">Sinks em ordem de tentativa (primeira a última)</param>
        public CompositeLogSink(IEnumerable<ILogSink> sinks)
        {
            if (sinks == null)
                throw new ArgumentNullException(nameof(sinks));

            // RFC: Lista imutável - ordem é preservada para sempre
            // Usamos Array.AsReadOnly para garantir imutabilidade real
            _sinks = sinks.ToList().AsReadOnly();
        }

        /// <summary>
        /// Escreve log único em todos os sinks sequencialmente.
        /// 
        /// RFC: Cada sink é tentado UMA VEZ.
        /// RFC: Falha em um sink NÃO impede próximos.
        /// RFC: Nenhuma exceção pode escapar.
        /// </summary>
        public void Write(ILogEntry entry)
        {
            // RFC: Guard rail - entrada nula é no-op
            if (entry == null)
                return;

            // RFC: Guard rail - sem sinks é no-op
            if (_sinks.Count == 0)
                return;

            // RFC: Processar cada sink em ORDEM FIXA
            // Justificativa do foreach: Garante ordem determinística, sem retry, sem loops
            foreach (var sink in _sinks)
            {
                try
                {
                    // RFC: Tentar sink - pode falhar
                    sink.Write(entry);

                    // RFC: Sucesso ou falha, NUNCA tenta este sink novamente neste flush
                    // (foreach avança para próximo sink)
                }
                catch
                {
                    // RFC: TODAS as exceções devem ser capturadas
                    // RFC: Falha em um sink NÃO afeta outros sinks
                    // RFC: Nenhuma exceção pode sair do CompositeLogSink
                    // RFC: Absorção silenciosa é intencional (observabilidade é externa)

                    // Próximo sink será tentado (foreach continua)
                }
            }

            // RFC: Método NUNCA lança exceção (mesmo se TODOS os sinks falharem)
        }

        /// <summary>
        /// Escreve batch de logs em todos os sinks sequencialmente.
        /// 
        /// RFC: Buffer entrega batch UMA VEZ.
        /// RFC: Fallback batch → individual já está implementado.
        /// RFC: Fallback entre sinks ocorre APÓS falha total de um sink.
        /// </summary>
        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            // RFC: Guard rail - entrada nula é no-op
            if (entries == null)
                return;

            // RFC: Guard rail - sem sinks é no-op
            if (_sinks.Count == 0)
                return;

            // RFC: Materializar batch UMA VEZ (evita múltiplas iterações)
            // Justificativa: IEnumerable pode ser query LINQ que reavalia a cada iteração
            var batch = entries as IList<ILogEntry> ?? entries.ToList();

            // RFC: Guard rail - batch vazio é no-op
            if (batch.Count == 0)
                return;

            // RFC: Processar cada sink em ORDEM FIXA
            foreach (var sink in _sinks)
            {
                try
                {
                    // RFC: Preferir WriteBatch se sink suportar
                    if (sink is IBatchLogSink batchSink)
                    {
                        // RFC: Tentar batch - pode falhar completamente
                        batchSink.WriteBatch(batch);
                    }
                    else
                    {
                        // RFC: Fallback batch → individual (dentro do MESMO sink)
                        // Justificativa: Sink não suporta batch, escreve item por item
                        // Nota: Isto NÃO é fallback ENTRE sinks
                        foreach (var entry in batch)
                        {
                            try
                            {
                                sink.Write(entry);
                            }
                            catch
                            {
                                // RFC: Absorve falha individual
                                // Justificativa: Tentar salvar máximo de logs possível
                                // Próximo entry será tentado
                            }
                        }
                    }

                    // RFC: Sucesso ou falha, NUNCA tenta este sink novamente neste flush
                }
                catch
                {
                    // RFC: TODAS as exceções devem ser capturadas
                    // RFC: Falha TOTAL de um sink → próximo sink será tentado
                    // 
                    // Exemplos de falha total:
                    // - WriteBatch lançou exceção
                    // - Sink não suporta batch E foreach interno falhou completamente
                    // - Timeout, IOException, OutOfMemoryException, etc.
                    //
                    // RFC: Nenhuma exceção pode sair do CompositeLogSink
                }
            }

            // RFC: Método NUNCA lança exceção (mesmo se TODOS os sinks falharem)
        }

        /// <summary>
        /// Escreve log único de forma assíncrona em todos os sinks sequencialmente.
        /// 
        /// RFC: Fallback respeita lifecycle (OPERATIONAL, STOPPING, DISPOSING).
        /// RFC: Nenhum sink é reativado após Dispose.
        /// </summary>
        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            // RFC: Guard rail - entrada nula é no-op
            if (entry == null)
                return;

            // RFC: Guard rail - sem sinks é no-op
            if (_sinks.Count == 0)
                return;

            // RFC: Processar cada sink em ORDEM FIXA
            // Nota: Processamento é SEQUENCIAL (await dentro do foreach)
            // Justificativa: Manter ordem determinística e facilitar debugging
            foreach (var sink in _sinks)
            {
                try
                {
                    // RFC: Preferir WriteAsync se sink suportar
                    if (sink is IAsyncLogSink asyncSink)
                    {
                        await asyncSink.WriteAsync(entry, cancellationToken);
                    }
                    else
                    {
                        // RFC: Fallback async → sync (dentro do MESMO sink)
                        // Justificativa: Sink não suporta async, usa método síncrono
                        sink.Write(entry);
                    }

                    // RFC: Sucesso ou falha, NUNCA tenta este sink novamente
                }
                catch (OperationCanceledException)
                {
                    // RFC: Cancellation é respeitado - para processamento imediatamente
                    // Justificativa: Token cancelado indica shutdown ou timeout externo
                    // Sinks restantes NÃO serão tentados (comportamento esperado)
                    return;
                }
                catch
                {
                    // RFC: TODAS as outras exceções devem ser capturadas
                    // RFC: Falha em um sink NÃO afeta outros sinks
                    // Próximo sink será tentado
                }
            }

            // RFC: Método NUNCA lança exceção (exceto OperationCanceledException)
        }

        /// <summary>
        /// Escreve batch de logs de forma assíncrona em todos os sinks sequencialmente.
        /// 
        /// RFC: Dispose() síncrono NÃO aguarda FlushAsync().
        /// RFC: FlushAsync em andamento pode ser abandonado durante shutdown.
        /// </summary>
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            // RFC: Guard rail - entrada nula é no-op
            if (entries == null)
                return;

            // RFC: Guard rail - sem sinks é no-op
            if (_sinks.Count == 0)
                return;

            // RFC: Materializar batch UMA VEZ
            var batch = entries as IList<ILogEntry> ?? entries.ToList();

            // RFC: Guard rail - batch vazio é no-op
            if (batch.Count == 0)
                return;

            // RFC: Processar cada sink em ORDEM FIXA (sequencial)
            foreach (var sink in _sinks)
            {
                try
                {
                    // RFC: Tentar na seguinte ordem de preferência:
                    // 1. IAsyncBatchLogSink (async + batch) - melhor performance
                    // 2. IBatchLogSink (sync + batch) - boa performance
                    // 3. ILogSink (sync + individual) - fallback completo

                    if (sink is IAsyncBatchLogSink asyncBatchSink)
                    {
                        // Caso 1: Async + Batch (ideal)
                        await asyncBatchSink.WriteBatchAsync(batch, cancellationToken);
                    }
                    else if (sink is IBatchLogSink batchSink)
                    {
                        // Caso 2: Sync + Batch (fallback de async → sync)
                        batchSink.WriteBatch(batch);
                    }
                    else
                    {
                        // Caso 3: Sync + Individual (fallback completo)
                        // RFC: Fallback batch → individual
                        foreach (var entry in batch)
                        {
                            try
                            {
                                sink.Write(entry);
                            }
                            catch
                            {
                                // RFC: Absorve falha individual
                                // Próximo entry será tentado
                            }
                        }
                    }

                    // RFC: Sucesso ou falha, NUNCA tenta este sink novamente
                }
                catch (OperationCanceledException)
                {
                    // RFC: Cancellation para processamento imediatamente
                    return;
                }
                catch
                {
                    // RFC: TODAS as outras exceções capturadas
                    // RFC: Falha TOTAL de um sink → próximo sink tentado
                }
            }

            // RFC: Método NUNCA lança exceção (exceto OperationCanceledException)
        }
    }
}