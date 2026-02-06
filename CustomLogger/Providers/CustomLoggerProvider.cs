using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.Configurations;
using CustomLogger.Scopes;
using CustomLogger.Sinks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace CustomLogger.Providers
{
    /// <summary>
    /// Provider responsável por criar instâncias do CustomLogger
    /// e orquestrar o ciclo de vida do logging.
    /// 
    /// RFC - Lifecycle: CREATED → OPERATIONAL → STOPPING → DISPOSING → DISPOSED
    /// </summary>
    public sealed class CustomLoggerProvider : ILoggerProvider
    {
        private readonly CustomProviderConfiguration _configuration;
        private readonly IAsyncLogBuffer _buffer;
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

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
                foreach (var s in sinksToTrack)
                {
                    if (s is IDisposable disposable)
                        _disposables.Add(disposable);
                }
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
        /// RFC: Sequência obrigatória: STOPPING → Timer.Dispose → DISPOSING → Flush → DISPOSED → Sinks.Dispose
        /// </summary>
        public void Dispose()
        {
            // GUARD RAIL #2: Tentar transitar para STOPPING
            // RFC: Apenas PRIMEIRA thread que chamar Dispose() deve executar shutdown
            // RFC: Chamadas subsequentes retornam imediatamente (idempotência)
            bool shouldShutdown = _lifecycle.TryTransitionToStopping();

            if (!shouldShutdown)
            {
                // RFC: Dispose() já foi chamado por outra thread, retorna silenciosamente
                return;
            }

            // ESTADO: STOPPING
            // RFC: A partir daqui, novos logs são rejeitados silenciosamente
            // RFC: CreateLogger() lançará ObjectDisposedException

            try
            {
                // PASSO 1: Parar timer de flush periódico
                // RFC: Timer deve ser parado ANTES do flush final
                // Justificativa: Evita race condition (timer chamando Flush durante Dispose)
                if (_buffer is IDisposable disposableBuffer)
                {
                    // InstanceLogBuffer.Dispose() para o timer internamente
                    // Nota: Aqui apenas paramos o timer, NÃO fazemos flush ainda
                    // O flush será feito explicitamente no próximo passo

                    // IMPORTANTE: Este Dispose do buffer NÃO deve fazer flush
                    // Buffer deve ter lógica separada: timer.Dispose() vs Flush()
                    // Por ora, assumindo que buffer tem método para parar timer sem flush
                    // (Se não tiver, é um BLOQUEIO - ver final do arquivo)
                    disposableBuffer.Dispose();
                }

                // PASSO 2: Transitar para DISPOSING
                // RFC: STOPPING → DISPOSING ocorre antes do flush final
                _lifecycle.TransitionToDisposing();

                // ESTADO: DISPOSING
                // RFC: Flush final deve acontecer APÓS timer parado

                // PASSO 3: Flush final
                // RFC: Drena TODOS os logs enfileirados até momento do STOPPING
                // RFC: Flush final é síncrono e best-effort
                _buffer.Flush();

                // PASSO 4: Transitar para DISPOSED
                // RFC: Após flush final, antes de liberar sinks
                _lifecycle.TransitionToDisposed();

                // ESTADO: DISPOSED
                // RFC: Recursos podem ser liberados agora

                // PASSO 5: Dispose dos sinks
                // RFC: Sinks são liberados POR ÚLTIMO (após receberem todos os logs)
                // RFC: Exceções de sinks são absorvidas (best-effort)
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
                    }
                }
            }
            catch
            {
                // RFC: Dispose() NUNCA lança exceções
                // Absorve qualquer exceção não-tratada (defesa em profundidade)
                // Em produção, isto não deve acontecer (todas as exceções já estão tratadas)
            }
        }
    }
}