using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    /// <summary>
    /// Wrapper que adiciona modo degradado a qualquer sink.
    /// 
    /// RFC - MODO DEGRADADO:
    /// - Estado POR SINK (não global)
    /// - Entrada: Primeira falha (N=1)
    /// - Saída: Primeiro sucesso
    /// - Comportamento: Continua tentando (não desiste)
    /// - Thread-safe: volatile bool (sem locks)
    /// </summary>
    public sealed class DegradableLogSink : ILogSink, IBatchLogSink, IAsyncLogSink, IAsyncBatchLogSink, IDisposable
    {
        private readonly ILogSink _innerSink;

        // RFC: Estado de degradação POR SINK
        // volatile garante visibilidade entre threads
        // Sem lock: leitura/escrita de bool é atômica
        private volatile bool _isDegraded;

        /// <summary>
        /// Indica se sink está atualmente em modo degradado.
        /// Thread-safe (volatile).
        /// </summary>
        public bool IsDegraded => _isDegraded;

        public DegradableLogSink(ILogSink innerSink)
        {
            _innerSink = innerSink ?? throw new ArgumentNullException(nameof(innerSink));
            _isDegraded = false; // Inicia SAUDÁVEL
        }

        /// <summary>
        /// Escreve log único com detecção de degradação.
        /// 
        /// RFC: Primeira falha → degradado
        /// RFC: Primeiro sucesso → recuperado
        /// RFC: Nunca lança exceção
        /// </summary>
        public void Write(ILogEntry entry)
        {
            if (entry == null)
                return;

            try
            {
                // RFC: Sink degradado CONTINUA sendo tentado
                // Não há skip, não há descarte
                _innerSink.Write(entry);

                // RFC: Sucesso → sair do modo degradado imediatamente
                if (_isDegraded)
                {
                    _isDegraded = false;
                }
            }
            catch
            {
                // RFC: Falha → entrar em modo degradado
                // N=1: primeira falha já marca como degradado
                if (!_isDegraded)
                {
                    _isDegraded = true;
                }

                // RFC: Modo degradado NUNCA lança exceção
                // Absorve silenciosamente (mesmo comportamento de antes)
            }
        }

        /// <summary>
        /// Escreve batch com detecção de degradação.
        /// 
        /// RFC: Fallback batch → individual já existe (CompositeLogSink)
        /// Aqui apenas detectamos falha TOTAL do batch.
        /// </summary>
        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            if (entries == null)
                return;

            if (!(_innerSink is IBatchLogSink batchSink))
            {
                // RFC: Sink não suporta batch → fallback para individual
                // Este é fallback de TIPO, não de degradação
                foreach (var entry in entries)
                {
                    Write(entry); // Já tem detecção de degradação
                }
                return;
            }

            try
            {
                // RFC: Sink degradado CONTINUA sendo tentado
                batchSink.WriteBatch(entries);

                // RFC: Sucesso → sair do modo degradado
                if (_isDegraded)
                {
                    _isDegraded = false;
                }
            }
            catch
            {
                // RFC: Falha TOTAL do batch → marcar como degradado
                if (!_isDegraded)
                {
                    _isDegraded = true;
                }

                // RFC: Fallback batch → individual (tentativa de salvar parcial)
                // Este é fallback de ESTRATÉGIA, não de degradação
                foreach (var entry in entries)
                {
                    try
                    {
                        _innerSink.Write(entry);
                    }
                    catch
                    {
                        // Absorve falha individual
                    }
                }

                // RFC: Modo degradado NUNCA lança exceção
            }
        }

        /// <summary>
        /// Escreve log de forma assíncrona com detecção de degradação.
        /// </summary>
        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null)
                return;

            if (!(_innerSink is IAsyncLogSink asyncSink))
            {
                // Fallback: async → sync
                Write(entry);
                return;
            }

            try
            {
                await asyncSink.WriteAsync(entry, cancellationToken);

                // Sucesso → recuperar
                if (_isDegraded)
                {
                    _isDegraded = false;
                }
            }
            catch (OperationCanceledException)
            {
                // RFC: Cancellation NÃO é falha de sink
                // Não marcar como degradado
                throw; // Propaga cancellation
            }
            catch
            {
                // Falha → degradar
                if (!_isDegraded)
                {
                    _isDegraded = true;
                }

                // Absorve exceção (exceto cancellation)
            }
        }

        /// <summary>
        /// Escreve batch de forma assíncrona com detecção de degradação.
        /// </summary>
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (entries == null)
                return;

            if (!(_innerSink is IAsyncBatchLogSink asyncBatchSink))
            {
                // Fallback: async batch → sync batch
                WriteBatch(entries);
                return;
            }

            try
            {
                await asyncBatchSink.WriteBatchAsync(entries, cancellationToken);

                // Sucesso → recuperar
                if (_isDegraded)
                {
                    _isDegraded = false;
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation não é falha
                throw;
            }
            catch
            {
                // Falha TOTAL → degradar
                if (!_isDegraded)
                {
                    _isDegraded = true;
                }

                // Fallback: tentar individual
                foreach (var entry in entries)
                {
                    try
                    {
                        if (_innerSink is IAsyncLogSink asyncSink)
                        {
                            await asyncSink.WriteAsync(entry, cancellationToken);
                        }
                        else
                        {
                            await Task.Run(() => _innerSink.Write(entry), cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // Absorve falha individual
                    }
                }
            }
        }

        /// <summary>
        /// Dispose do sink interno.
        /// 
        /// RFC: Modo degradado NÃO interfere no shutdown.
        /// </summary>
        public void Dispose()
        {
            // RFC: Dispose do sink interno (se IDisposable)
            if (_innerSink is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // RFC: Dispose NUNCA lança exceção
                }
            }

            // Estado de degradação é irrelevante após dispose
            // Não precisa limpar _isDegraded
        }
    }
}