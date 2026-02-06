using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            if (_options.BatchOptions.FlushInterval > TimeSpan.Zero)
            {
                _flushTimer = new Timer(
                    _ => Flush(),
                    null,
                    _options.BatchOptions.FlushInterval,
                    _options.BatchOptions.FlushInterval
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
            if (!_options.UseGlobalBuffer)
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
            // GUARD RAIL: Idêntico a Enqueue()
            if (IsDisposed() || entry == null)
            {
                return; // Rejeita silenciosamente
            }

            // Modo sem buffer: escreve diretamente
            if (!_options.UseGlobalBuffer)
            {
                if (_sink is IAsyncLogSink asyncSink)
                {
                    try
                    {
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
                await FlushAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Processa todos os logs enfileirados.
        /// 
        /// RFC: Flush explícito retorna silenciosamente se disposed.
        /// RFC: Flush implícito (do Dispose) sempre executa.
        /// 
        /// IMPORTANTE: Implementação STUB - apenas estrutura de guard rails.
        /// Lógica completa de flush, fallback e isolamento de sinks NÃO implementada.
        /// </summary>
        public void Flush()
        {
            // GUARD RAIL: Verificar se disposed
            // NOTA: Provider gerencia quando flush pode ocorrer via ProviderLifecycleManager.CanFlush()
            // Buffer faz verificação defensiva adicional
            if (IsDisposed())
            {
                // EXCEÇÃO: Flush chamado pelo Dispose do Provider DEVE executar
                // Como saber se é flush implícito vs explícito?
                // BLOQUEIO IDENTIFICADO - ver final do arquivo
                return; // Por ora, retorna silenciosamente
            }

            lock (_flushLock)
            {
                // STUB: Lógica real de flush não implementada
                // Apenas drena a fila sem processar

                if (_queue.IsEmpty)
                    return;

                var batch = new List<ILogEntry>();
                while (_queue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                // TODO: Processar batch com isolamento de sinks, fallback, etc.
                // Por ora, apenas stub
            }
        }

        /// <summary>
        /// Flush assíncrono.
        /// 
        /// RFC: Dispose() síncrono NÃO aguarda FlushAsync().
        /// RFC: FlushAsync em andamento pode ser abandonado durante shutdown.
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // GUARD RAIL: Idêntico a Flush()
            if (IsDisposed())
            {
                return; // Retorna silenciosamente
            }

            await Task.Run(() =>
            {
                lock (_flushLock)
                {
                    // STUB: Lógica real de flush assíncrono não implementada
                    if (_queue.IsEmpty)
                        return;

                    var batch = new List<ILogEntry>();
                    while (_queue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }

                    // TODO: Processar batch de forma assíncrona
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Libera recursos do buffer.
        /// 
        /// RFC: Idempotente.
        /// RFC: Para timer ANTES de flush final.
        /// RFC: Flush final deve ser chamado EXTERNAMENTE pelo Provider.
        /// </summary>
        public void Dispose()
        {
            // GUARD RAIL: Idempotência
            // Usa Interlocked.CompareExchange para thread-safety
            int wasDisposed = Interlocked.CompareExchange(ref _isDisposed, 1, 0);

            if (wasDisposed == 1)
            {
                // Já foi disposed, retorna silenciosamente
                return;
            }

            // Agora estamos disposed (_isDisposed = 1)

            // PASSO 1: Parar timer
            // RFC: Timer deve ser parado ANTES do flush final
            // RFC: Flush final será chamado pelo Provider, NÃO aqui
            _flushTimer?.Dispose();

            // RFC: Buffer.Dispose() NÃO deve fazer flush
            // Flush final é responsabilidade do Provider (chamado explicitamente após timer parado)

            // Recursos liberados
            // Fila permanece com logs pendentes (serão processados pelo Provider via Flush explícito)
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