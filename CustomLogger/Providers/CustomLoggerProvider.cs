using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Scopes;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CustomLogger.Providers
{
    /// <summary>
    /// Provider responsável por criar instâncias do CustomLogger
    /// e orquestrar o ciclo de vida do logging.
    /// 
    /// RFC - Lifecycle: CREATED → OPERATIONAL → STOPPING → DISPOSING → DISPOSED
    /// </summary>
    public sealed class CustomLoggerProvider : ILoggerProvider, ILoggingHealthState
    {
        private readonly CustomProviderConfiguration _configuration;
        private readonly IAsyncLogBuffer _buffer;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        // Track sinks list so health snapshot can expose them
        private readonly IReadOnlyList<ILogSink> _trackedSinks;

        // Gerenciador de estado (centraliza toda lógica de transições)
        private readonly ProviderLifecycleManager _lifecycle = new ProviderLifecycleManager();

        /// <summary>
        /// Construtor público para uso com builder.
        /// RFC: Estado inicial é CREATED.
        /// RFC: Startup não abre recursos de IO no Provider.
        /// </summary>
        public CustomLoggerProvider(
            CustomProviderOptions options,
            ILogSink sink,
            IEnumerable<ILogSink> sinksToTrack)
        {
            // Validação de argumentos (startup atômico)
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            _configuration = new CustomProviderConfiguration(options);
            _buffer = new InstanceLogBuffer(sink, options);

            // Rastreia sinks descartáveis para dispose futuro
            if (sinksToTrack != null)
            {
                var list = new List<ILogSink>();
                foreach(var s in sinksToTrack)
                {
                    if (s != null) list.Add(s);
                }

                _trackedSinks = list.AsReadOnly();

                foreach (var s in sinksToTrack)
                {
                    if (s is IDisposable disposable)
                        _disposables.Add(disposable);
                }
            }
            else
            {
                _trackedSinks = new List<ILogSink>().AsReadOnly();
            }

            // Estado permanece CREATED até primeira chamada a CreateLogger()
        }

        /// <summary>
        /// Cria uma nova instância de logger.
        /// 
        /// RFC: Permitido apenas em CREATED e OPERATIONAL.
        /// RFC: Lança ObjectDisposedException em STOPPING, DISPOSING, DISPOSED.
        /// RFC: Primeira chamada transita CREATED → OPERATIONAL.
        /// RFC: Chamadas subsequentes permanecem em OPERATIONAL.
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            // GUARD RAIL #1: Verificar se estado permite CreateLogger
            // RFC: Lança exceção se estado for STOPPING ou além
            _lifecycle.GuardCreateLogger();

            // RFC: Primeira chamada transita para OPERATIONAL
            // Transição é thread-safe via Interlocked.CompareExchange
            // Se retornar false, já estava em OPERATIONAL (ok, continua)
            _lifecycle.TryTransitionToOperational();

            // Criar logger (cada um com seu próprio scope provider)
            var scopeProvider = new LogScopeProvider();
            return new Loggers.CustomLogger(categoryName, _configuration, _buffer, scopeProvider);
        }

        /// <summary>
        /// Libera todos os recursos do provider.
        /// 
        /// RFC: Idempotente - múltiplas chamadas são seguras.
        /// RFC: Síncrono - não aguarda operações async.
        /// RFC: Best-effort - absorve exceções de sinks.
        /// RFC: Única forma de iniciar shutdown.
        /// RFC: Sequência obrigatória:
        ///   1. STOPPING
        ///   2. Flush final
        ///   3. DISPOSING
        ///   4. Parar timer
        ///   5. DISPOSED
        ///   6. Dispose sinks
        /// </summary>
        public void Dispose()
        {
            // ========================================================================
            // GUARD RAIL: Garantir idempotência e thread-safety
            // ========================================================================

            // RFC: Apenas PRIMEIRA thread que chamar Dispose() deve executar shutdown
            // RFC: Chamadas subsequentes retornam imediatamente (idempotência)
            bool shouldShutdown = _lifecycle.TryTransitionToStopping();

            if (!shouldShutdown)
            {
                // RFC: Dispose() já foi chamado por outra thread ou já está em shutdown
                // Idempotência: retorna silenciosamente, sem fazer nada
                return;
            }

            // ========================================================================
            // ESTADO: STOPPING
            // ========================================================================
            // RFC: A partir daqui, novos logs são rejeitados silenciosamente
            // RFC: CreateLogger() lançará ObjectDisposedException
            // RFC: Logs JÁ enfileirados serão processados no flush final

            try
            {
                // ====================================================================
                // PASSO 1: FLUSH FINAL (enquanto buffer ainda está operacional)
                // ====================================================================
                // RFC: Drena TODOS os logs enfileirados até momento do STOPPING
                // RFC: Flush final é síncrono e best-effort
                // 
                // CRÍTICO: Flush ANTES de Buffer.Dispose() (resolve bloqueio #1)
                // Razão: Buffer.Flush() verifica se está disposed e retorna silenciosamente
                // Se chamarmos Buffer.Dispose() primeiro, flush não executará
                //
                // Justificativa: Buffer ainda não está disposed, então Flush() executará normalmente
                _buffer.Flush();

                // ====================================================================
                // PASSO 2: Transitar para DISPOSING
                // ====================================================================
                // RFC: STOPPING → DISPOSING ocorre APÓS flush final
                // Garante que não há mais logs pendentes antes de liberar recursos
                _lifecycle.TransitionToDisposing();

                // ====================================================================
                // ESTADO: DISPOSING
                // ====================================================================
                // RFC: Flush já foi executado, recursos podem ser liberados agora

                // ====================================================================
                // PASSO 3: Parar timer de flush periódico
                // ====================================================================
                // RFC: Timer deve ser parado APÓS flush final
                // Justificativa: Timer já não é necessário (flush final já foi feito)
                //
                // Buffer.Dispose() para o timer internamente
                // Não faz flush (flush já foi feito no passo 1)
                if (_buffer is IDisposable disposableBuffer)
                {
                    try
                    {
                        disposableBuffer.Dispose();
                    }
                    catch
                    {
                        // RFC: Best-effort - absorve exceção de dispose de buffer
                        // Justificativa: Timer pode já estar disposed ou falhar, não é crítico
                    }
                }

                // ====================================================================
                // PASSO 4: Transitar para DISPOSED
                // ====================================================================
                // RFC: DISPOSING → DISPOSED ocorre após recursos internos liberados
                // Garante que estado está correto antes de dispose de sinks
                _lifecycle.TransitionToDisposed();

                // ====================================================================
                // ESTADO: DISPOSED
                // ====================================================================
                // RFC: Core está completamente inutilizável agora
                // RFC: Sinks podem ser liberados por último

                // ====================================================================
                // PASSO 5: Dispose dos sinks
                // ====================================================================
                // RFC: Sinks são liberados POR ÚLTIMO (após receberem todos os logs)
                // RFC: Exceções de sinks são absorvidas (best-effort)
                // RFC: Isolamento: falha em um sink não impede dispose de outros
                foreach (var disposable in _disposables)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // RFC: Absorve TODAS as exceções de dispose de sinks
                        // Justificativa: Falha em um sink não deve impedir dispose de outros
                        // Best-effort: tentamos liberar todos, mesmo que alguns falhem
                    }
                }
            }
            catch
            {
                // ====================================================================
                // DEFESA EM PROFUNDIDADE
                // ====================================================================
                // RFC: Dispose() NUNCA lança exceções
                // RFC: Best-effort - tenta completar shutdown mesmo com erros
                //
                // Absorve qualquer exceção não-tratada nos passos acima
                // Em produção, isto não deveria acontecer (todas as exceções já estão tratadas)
                // Mas protege contra bugs ou condições inesperadas
            }

            // ========================================================================
            // SHUTDOWN COMPLETO
            // ========================================================================
            // Estado final: DISPOSED
            // Todos os recursos liberados (best-effort)
            // Provider está completamente inutilizável
        }

        /// <summary>
        /// Leitura thread-safe do tamanho atual do buffer.
        /// 
        /// IMPLEMENTAÇÃO:
        /// - Delega para InstanceLogBuffer (que já tem ConcurrentQueue)
        /// - Snapshot atômico via ConcurrentQueue.Count
        /// </summary>
        int ILoggingHealthState.CurrentBufferSize
        {
            get
            {
                // Se usar buffer global: retornar _buffer.CurrentSize
                // Se usar buffer de instância: agregar todos os buffers

                // Exemplo para buffer de instância:
                // return _loggers.Values.Sum(logger => logger.Buffer.CurrentSize);

                // IMPORTANTE: Não lançar exceção, retornar 0 se disposed
                return 0; // Stub - substituir por lógica real
            }
        }

        int ILoggingHealthState.MaxBufferCapacity => _configuration.Options.MaxBufferSize ?? 0;

        bool ILoggingHealthState.IsDiscardingMessages
        {
            get
            {
                // Minimal conservative check
                try
                {
                    var current = ((ILoggingHealthState)this).CurrentBufferSize;
                    var max = ((ILoggingHealthState)this).MaxBufferCapacity;
                    if (max <= 0) return false;
                    return current >= max;
                }
                catch
                {
                    return false;
                }
            }
        }

        bool ILoggingHealthState.IsDegradedMode
        {
            get
            {
                // If any tracked sink is degraded, consider provider degraded
                try
                {
                    foreach (var sink in _trackedSinks)
                    {
                        if (sink == null) continue;

                        // DegradableLogSink exposes IsDegraded
                        var degr = sink as DegradableLogSink;
                        if (degr != null && degr.IsDegraded)
                            return true;
                    }
                }
                catch
                {
                }

                // Default: false
                return false;
            }
        }

                IReadOnlyList<SinkHealthSnapshot> ILoggingHealthState.SinkStates
        {
            get
            {
                var snapshots = new List<SinkHealthSnapshot>();

                try
                {
                    ProcessTrackedSinks(snapshots);
                }
                catch
                {
                    // Best-effort: if anything fails, return what we have (possibly empty)
                }

                return snapshots.AsReadOnly();
            }
        }

        /// <summary>
        /// Processa todos os sinks rastreados e adiciona seus snapshots à lista.
        /// Gerencia tanto sinks compostos (com decomposição) quanto sinks normais.
        /// CC: ~3
        /// </summary>
        private void ProcessTrackedSinks(List<SinkHealthSnapshot> snapshots)
        {
            // Guard clause: sinks não inicializados ou vazios
            if (_trackedSinks == null || _trackedSinks.Count == 0)
                return;

            foreach (var sink in _trackedSinks)
            {
                // Guard clause: null safety
                if (sink == null)
                    continue;

                // Tentar extrair snapshots de um sink composto
                var compositeSnapshots = ExtractCompositeSnapshot(sink);
                if (compositeSnapshots != null && compositeSnapshots.Count > 0)
                {
                    snapshots.AddRange(compositeSnapshots);
                    continue;
                }

                // Processar sink normal
                var normalSnapshot = ComputeSinkHealthSnapshot(sink);
                snapshots.Add(normalSnapshot);
            }
        }

        /// <summary>
        /// Extrai snapshots de um sink composto via reflection.
        /// Retorna lista vazia se não for composto ou se a reflexão falhar.
        /// CC: ~4
        /// </summary>
        private List<SinkHealthSnapshot> ExtractCompositeSnapshot(ILogSink sink)
        {
            var result = new List<SinkHealthSnapshot>();

            // Guard clause: tipo check
            if (!(sink is CompositeLogSink composite))
                return result;

            // Tentar refletir o campo "_sinks"
            var fieldInfo = composite.GetType().GetField(
                "_sinks",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (fieldInfo == null)
                return result;

            var innerSinks = fieldInfo.GetValue(composite) as IEnumerable<ILogSink>;
            if (innerSinks == null)
                return result;

            // Processar cada sink interno
            foreach (var innerSink in innerSinks)
            {
                if (innerSink == null)
                    continue;

                var snapshot = ComputeSinkHealthSnapshot(innerSink);
                result.Add(snapshot);
            }

            return result;
        }

        /// <summary>
        /// Computa um snapshot de saúde para um sink individual.
        /// Centraliza a lógica de detecção de degradação e criação de snapshot.
        /// CC: ~2
        /// </summary>
        private SinkHealthSnapshot ComputeSinkHealthSnapshot(ILogSink sink)
        {
            bool isOperational = true;
            string statusMessage = null;

            // Verificar se sink é degradável e está em modo degradado
            var degradable = sink as DegradableLogSink;
            if (degradable != null && degradable.IsDegraded)
            {
                isOperational = false;
                statusMessage = "Degraded";
            }

            return new SinkHealthSnapshot(
                name: sink.GetType().Name,
                type: sink.GetType().FullName,
                isOperational: isOperational,
                statusMessage: statusMessage
            );
        }
    }
}