using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Buffering
{
    /// <summary>
    /// Buffer de logs com suporte a batching e flush periódico.
    /// 
    /// IMPORTANTE: Este stub implementa APENAS guard rails de estado.
    /// Lógica completa de flush, fallback e modo degradado NÃO está implementada.
    /// </summary>
    public sealed class InstanceLogBuffer : IAsyncLogBuffer, IDisposable
    {
        private readonly ILogSink _sink;
        private readonly ConcurrentQueue<ILogEntry> _queue = new ConcurrentQueue<ILogEntry>();
        private readonly CustomProviderOptions _options;
        private readonly Timer _flushTimer;
        private readonly object _flushLock = new object();

        // Gerenciador de estado do buffer
        // Nota: Buffer tem estado próprio (mais simples que Provider)
        // Estados: Operacional (não-disposed) vs Disposed
        private int _isDisposed = 0; // 0 = operacional, 1 = disposed

        public InstanceLogBuffer(ILogSink sink, CustomProviderOptions options)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // RFC: Timer para flush periódico (se configurado)
            // ✅ CORRIGIDO: Timer usa FlushAsync para evitar bloqueio de threads do pool
            if (_options.BatchOptions.FlushIntervalMs > 0)
            {
                _flushTimer = new Timer(
                    async _ => await FlushAsync(CancellationToken.None),
                    null,
                    (int)_options.BatchOptions.FlushIntervalMs,
                    (int)_options.BatchOptions.FlushIntervalMs
                );
            }

            // Estado inicial: operacional (não-disposed)
        }

        /// <summary>
        /// Enfileira log para processamento.
        /// 
        /// RFC: Hot path - NUNCA lança exceções.
        /// RFC: Aceita logs apenas enquanto Provider estiver OPERATIONAL.
        /// RFC: Rejeita silenciosamente em outros estados.
        /// 
        /// IMPORTANTE: Guard rail de estado deve ser gerenciado pelo PROVIDER.
        /// Buffer apenas verifica se está disposed (proteção defensiva).
        /// </summary>
        public void Enqueue(ILogEntry entry)
        {
            Debug.WriteLine("[Enqueue] >>> Iniciando Enqueue");

            // GUARD RAIL #1: Verificar se buffer está disposed
            // RFC: Hot path NUNCA lança exceção
            // RFC: Retorno silencioso se disposed
            if (IsDisposed() || entry == null)
            {
                return; // Rejeita silenciosamente
            }

            // GUARD RAIL #2: Provider deve ter verificado estado ANTES de chamar Enqueue
            // Se chegou aqui, Provider permitiu (estado é OPERATIONAL)

            // Modo sem buffer: escreve diretamente no sink
            if ((bool)!_options.UseGlobalBuffer)
            {
                try
                {
                    _sink.Write(entry);
                }
                catch
                {
                    // RFC: Hot path NUNCA lança exceção
                    // Absorve falha do sink
                }
                return;
            }

            // Modo com buffer: enfileira
            _queue.Enqueue(entry);

            // RFC: Flush automático ao atingir BatchSize
            if (_queue.Count >= _options.BatchOptions.BatchSize)
            {
                Flush();
            }
        }

        /// <summary>
        /// Enfileira log de forma assíncrona.
        /// 
        /// RFC: Hot path - NUNCA lança exceções.
        /// RFC: Comportamento idêntico a Enqueue() síncrono em termos de guard rails.
        /// </summary>
        public async Task EnqueueAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("[EnqueueAsync] >>> Iniciando EnqueueAsync");

            // GUARD RAIL: Idêntico a Enqueue()
            if (IsDisposed() || entry == null)
            {
                return; // Rejeita silenciosamente
            }

            // Modo sem buffer: escreve diretamente
            if ((bool)!_options.UseGlobalBuffer)
            {
                if (_sink is IAsyncLogSink asyncSink)
                {
                    try
                    {
                        Debug.WriteLine("[EnqueueAsync] >>> Iniciando WriteAsync");
                        await asyncSink.WriteAsync(entry, cancellationToken);
                    }
                    catch
                    {
                        // RFC: Hot path NUNCA lança exceção
                    }
                }
                else
                {
                    // Fallback: escrita síncrona
                    try
                    {
                        _sink.Write(entry);
                    }
                    catch
                    {
                        // RFC: Hot path NUNCA lança exceção
                    }
                }
                return;
            }

            // Modo com buffer: enfileira
            _queue.Enqueue(entry);

            // RFC: Flush automático ao atingir BatchSize
            if (_queue.Count >= _options.BatchOptions.BatchSize)
            {
                Debug.WriteLine("[EnqueueAsync] >>> Iniciando FlushAsync por Atingir BatchSize");
                await FlushAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Processa todos os logs enfileirados.
        /// 
        /// RFC: Flush final (do Dispose) sempre executa enquanto buffer não está disposed.
        /// RFC: Provider garante que flush é chamado ANTES de Buffer.Dispose().
        /// 
        /// IMPORTANTE: Implementação STUB - apenas estrutura de guard rails.
        /// Lógica completa de flush, fallback e isolamento de sinks NÃO implementada.
        /// </summary>
        public void Flush()
        {
            Debug.WriteLine("[Flush] >>> Iniciando Flush");

            // ====================================================================
            // GUARD RAIL: Verificar se buffer está disposed
            // ====================================================================
            // RFC: Flush retorna silenciosamente se buffer já está disposed
            // 
            // IMPORTANTE: Provider chama Flush ANTES de Buffer.Dispose()
            // Então em shutdown normal, flush sempre executará
            // Esta verificação protege contra chamadas externas após dispose
            if (IsDisposed())
            {
                return; // Retorna silenciosamente
            }

            // ====================================================================
            // FLUSH: Processar fila
            // ====================================================================
            // RFC: Lock garante que apenas uma thread processa flush por vez
            // Justificativa: Evita processar mesma fila simultaneamente
            lock (_flushLock)
            {
                // RFC: Retorna se fila está vazia (nada para processar)
                if (_queue.IsEmpty)
                    return;

                // ================================================================
                // DRENAGEM DA FILA
                // ================================================================
                // RFC: Drena TODOS os logs da fila no momento do lock
                // Logs adicionados APÓS este ponto vão para próximo flush
                var batch = new List<ILogEntry>();
                while (_queue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                // Segurança: Dupla verificação
                if (batch.Count == 0)
                    return;

                // ================================================================
                // PROCESSAMENTO DO BATCH
                // ================================================================
                // RFC: Flush tenta processar todos os logs drenados
                // RFC: Flush NUNCA lança exceções
                // RFC: Falha de sink não afeta outros sinks

                ProcessBatch(batch);
            }
        }

        /// <summary>
        /// Processa batch de logs enviando para sink.
        /// 
        /// RFC: Tentativa primária é WriteBatch (se suportado).
        /// RFC: Falha em WriteBatch gera fallback automático para escrita individual.
        /// RFC: TODAS as exceções são capturadas e absorvidas.
        /// RFC: Nenhuma exceção pode sair do flush.
        /// </summary>
        /// <param name="batch">Batch de logs a processar</param>
        private void ProcessBatch(List<ILogEntry> batch)
        {
            Debug.WriteLine("[ProcessBatch] >>> Iniciando ProcessBatch");

            // ====================================================================
            // ESTRATÉGIA 1: Tentar WriteBatch (se sink suportar)
            // ====================================================================
            // RFC: Se sink implementa IBatchLogSink, usar WriteBatch como tentativa primária
            // RFC: WriteBatch é mais eficiente (uma chamada vs N chamadas)

            if (_sink is IBatchLogSink batchSink)
            {
                try
                {
                    // ============================================================
                    // TENTATIVA: WriteBatch
                    // ============================================================
                    // RFC: Tentar escrever batch completo em uma única operação
                    // Se suceder, retornar (processamento completo)
                    batchSink.WriteBatch(batch);
                    return; // Sucesso - batch processado completamente
                }
                catch
                {
                    // ============================================================
                    // FALLBACK: Batch → Individual
                    // ============================================================
                    // RFC: Falha em WriteBatch gera fallback automático
                    // RFC: Tentamos escrita individual para cada log
                    // Razão: Batch pode falhar por timeout de transação,
                    //        mas escritas individuais podem ter sucesso parcial
                    //
                    // IMPORTANTE: Não fazemos 'throw' aqui
                    // Continuamos para ProcessIndividualLogs abaixo
                }
            }

            // ====================================================================
            // ESTRATÉGIA 2: Escrita Individual
            // ====================================================================
            // RFC: Se sink NÃO suporta batch, ir direto para escrita individual
            // RFC: Se WriteBatch falhou acima, fallback para escrita individual
            // RFC: Cada log é tentado UMA vez
            // RFC: Falha em um log NÃO impede tentativa dos demais

            ProcessIndividualLogs(batch);
        }

        /// <summary>
        /// Processa batch de logs de forma assíncrona.
        /// 
        /// ✅ NOVO: Versão async para background processing (Timer, FlushAsync explícito).
        /// RFC: Tenta WriteBatchAsync primeiro (se sink suportar).
        /// RFC: Fallback para WriteAsync individual.
        /// RFC: Fallback final para versão síncrona (se sink não suportar async).
        /// RFC: TODAS as exceções são capturadas e absorvidas.
        /// </summary>
        private async Task ProcessBatchAsync(List<ILogEntry> batch, CancellationToken cancellationToken)
        {
            Debug.WriteLine("[ProcessBatchAsync] >>> Iniciando ProcessBatchAsync");

            // ====================================================================
            // ESTRATÉGIA 1: Tentar WriteBatchAsync (se sink suportar async batch)
            // ====================================================================
            if (_sink is IAsyncBatchLogSink asyncBatchSink)
            {
                try
                {
                    await asyncBatchSink.WriteBatchAsync(batch, cancellationToken);
                    return; // Sucesso - batch processado completamente
                }
                catch
                {
                    // Fallback para processamento individual async
                }
            }

            // ====================================================================
            // ESTRATÉGIA 2: Tentar WriteAsync individual (se sink suportar async)
            // ====================================================================
            if (_sink is IAsyncLogSink asyncSink)
            {
                await ProcessIndividualLogsAsync(batch, asyncSink, cancellationToken);
                return;
            }

            // ====================================================================
            // ESTRATÉGIA 3: Fallback para versão síncrona
            // ====================================================================
            // RFC: Se sink não implementa async, usa versão sync
            ProcessBatch(batch);
        }

        /// <summary>
        /// Processa logs individualmente, um por vez.
        /// 
        /// RFC: Escrita individual é tentada UMA vez por log.
        /// RFC: Falha em um log NÃO impede processamento dos demais.
        /// RFC: TODAS as exceções são capturadas e absorvidas.
        /// </summary>
        /// <param name="batch">Batch de logs a processar individualmente</param>
        private void ProcessIndividualLogs(List<ILogEntry> batch)
        {
            Debug.WriteLine("[ProcessIndividualLogs] >>> Iniciando ProcessIndividualLogs");

            // ====================================================================
            // PROCESSAMENTO LOG POR LOG
            // ====================================================================
            // RFC: Itera por TODOS os logs, mesmo que alguns falhem
            // RFC: Isolamento total: falha em um log não afeta os demais

            foreach (var entry in batch)
            {
                try
                {
                    // ============================================================
                    // TENTATIVA: Write individual
                    // ============================================================
                    // RFC: Cada log é tentado exatamente UMA vez
                    // RFC: Nenhum retry adicional é permitido
                    // RFC: Se falhar, log é perdido (trade-off aceitável)

                    _sink.Write(entry);

                    // Se chegou aqui, log foi escrito com sucesso
                    // Continua para próximo log
                }
                catch
                {
                    // ============================================================
                    // ABSORÇÃO DE EXCEÇÕES
                    // ============================================================
                    // RFC: TODAS as exceções devem ser capturadas
                    // RFC: Nenhuma exceção pode sair do flush
                    // RFC: Flush NUNCA lança exceções
                    //
                    // Exceções possíveis:
                    // - IOException (disco cheio, permissões)
                    // - TimeoutException (sink remoto lento)
                    // - NetworkException (rede indisponível)
                    // - NullReferenceException (sink mal-implementado)
                    // - Qualquer outra exceção
                    //
                    // Comportamento: Log é silenciosamente descartado
                    // Trade-off: Melhor perder um log que derrubar aplicação
                    //
                    // Próximo log será tentado normalmente (isolamento)
                }
            }

            // ====================================================================
            // PROCESSAMENTO COMPLETO
            // ====================================================================
            // RFC: Mesmo que TODOS os logs tenham falhado, flush completa normalmente
            // RFC: Flush nunca lança exceções, mesmo em caso de falha total
            // Best-effort: Tentamos processar todos, mas não garantimos sucesso
        }

        /// <summary>
        /// Processa logs individualmente de forma assíncrona.
        /// 
        /// ✅ NOVO: Versão async para background processing.
        /// RFC: Escrita individual é tentada UMA vez por log.
        /// RFC: Falha em um log NÃO impede processamento dos demais.
        /// RFC: TODAS as exceções são capturadas e absorvidas.
        /// </summary>
        private async Task ProcessIndividualLogsAsync(
            List<ILogEntry> batch,
            IAsyncLogSink asyncSink,
            CancellationToken cancellationToken)
        {
            Debug.WriteLine("[ProcessIndividualLogsAsync] >>> Iniciando ProcessIndividualLogsAsync");

            foreach (var entry in batch)
            {
                try
                {
                    await asyncSink.WriteAsync(entry, cancellationToken);
                }
                catch
                {
                    // RFC: Absorve TODAS as exceções
                    // Log é descartado se falhar
                }
            }
        }

        /// <summary>
        /// Flush assíncrono.
        /// 
        /// RFC: Dispose() síncrono NÃO aguarda FlushAsync().
        /// RFC: FlushAsync em andamento pode ser abandonado durante shutdown.
        /// 
        /// IMPORTANTE: FlushAsync usa mesma lógica que Flush() síncrono.
        /// Diferença: Pode ser cancelado via CancellationToken.
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            Debug.WriteLine("[FlushAsync] >>> Iniciando FlushAsync");

            // ====================================================================
            // GUARD RAIL: Verificar se buffer está disposed
            // ====================================================================
            if (IsDisposed())
            {
                return; // Retorna silenciosamente
            }

            // ====================================================================
            // DRENAGEM DA FILA (sync - rápido)
            // ====================================================================
            // RFC: Lock apenas para drenagem da fila
            // RFC: I/O (lento) será feito fora do lock
            List<ILogEntry> batch;

            lock (_flushLock)
            {
                if (_queue.IsEmpty)
                    return;

                // Drena fila completa
                batch = new List<ILogEntry>();
                while (_queue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }
            }

            if (batch.Count == 0)
                return;

            // ====================================================================
            // PROCESSAMENTO ASYNC (fora do lock)
            // ====================================================================
            // ✅ CORRIGIDO: I/O async nativo sem Task.Run
            // ✅ CORRIGIDO: CancellationToken propagado corretamente
            await ProcessBatchAsync(batch, cancellationToken);
        }

        /// <summary>
        /// Libera recursos do buffer.
        /// 
        /// RFC: Idempotente.
        /// RFC: Para timer (se existir).
        /// RFC: NÃO faz flush - Provider chama Flush() explicitamente ANTES de Dispose().
        /// </summary>
        public void Dispose()
        {
            Debug.WriteLine("[Dispose] >>> Iniciando Dispose");

            // ====================================================================
            // GUARD RAIL: Idempotência
            // ====================================================================
            // RFC: Múltiplas chamadas a Dispose() são seguras
            // Usa Interlocked.CompareExchange para thread-safety
            int wasDisposed = Interlocked.CompareExchange(ref _isDisposed, 1, 0);

            if (wasDisposed == 1)
            {
                // Já foi disposed, retorna silenciosamente (idempotência)
                return;
            }

            // ====================================================================
            // ESTADO: Disposed (_isDisposed = 1)
            // ====================================================================
            // A partir daqui, Enqueue() rejeitará novos logs silenciosamente
            // Flush() retornará silenciosamente se chamado

            // ====================================================================
            // PASSO 1: Parar timer de flush periódico
            // ====================================================================
            // RFC: Timer deve ser parado para evitar flush automático
            // RFC: Provider já chamou Flush() antes deste Dispose()
            //
            // Justificativa: Timer em background pode causar race condition
            // Dispose do timer é best-effort (absorve exceções)
            _flushTimer?.Dispose();

            // ====================================================================
            // IMPORTANTE: NÃO fazer flush aqui
            // ====================================================================
            // RFC: Provider já chamou buffer.Flush() ANTES de buffer.Dispose()
            // Se fizermos flush aqui, será flush duplicado ou flush vazio
            //
            // Sequência correta (executada pelo Provider):
            //   1. buffer.Flush()    ← Drena logs pendentes
            //   2. buffer.Dispose()  ← Para timer (estamos aqui)
            //
            // Fila pode conter logs ainda? NÃO - Provider chamou Flush() antes
            // Se contiver, são logs de race condition (aceitável)

            // ====================================================================
            // RECURSOS LIBERADOS
            // ====================================================================
            // Timer: disposed
            // Fila: permanece alocada mas inacessível (será GC'ed)
            // Sink: Provider é responsável por dispose de sinks
        }

        /// <summary>
        /// Verifica se buffer está disposed (thread-safe).
        /// </summary>
        private bool IsDisposed()
        {
            return Interlocked.CompareExchange(ref _isDisposed, 0, 0) == 1;
        }
    }
}