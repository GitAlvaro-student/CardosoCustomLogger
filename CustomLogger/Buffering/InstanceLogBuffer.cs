using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Buffering
{
    public sealed class InstanceLogBuffer : IAsyncLogBuffer, IDisposable
    {
        private readonly ILogSink _sink;
        private readonly ConcurrentQueue<ILogEntry> _queue = new ConcurrentQueue<ILogEntry>();
        private readonly CustomProviderOptions _options;
        private readonly Timer _flushTimer;
        private readonly object _flushLock = new object();
        private readonly SemaphoreSlim _blockingSemaphore;
        private bool _disposed;
        private long _droppedLogsCount;

        public InstanceLogBuffer(ILogSink sink, CustomProviderOptions options)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (_options.BackpressureOptions.OverflowStrategy == OverflowStrategy.Block)
            {
                _blockingSemaphore = new SemaphoreSlim(
                    _options.BackpressureOptions.MaxQueueCapacity,
                    _options.BackpressureOptions.MaxQueueCapacity
                );
            }

            // ✅ Timer para flush periódico
            if (_options.BatchOptions.FlushInterval > TimeSpan.Zero)
            {
                _flushTimer = new Timer(
                    _ => Flush(),
                    null,
                    _options.BatchOptions.FlushInterval,
                    _options.BatchOptions.FlushInterval
                );
            }
        }

        public void Enqueue(ILogEntry entry)
        {
            if (_disposed || entry == null)
                return;

            if (!_options.UseGlobalBuffer)
            {
                _sink.Write(entry);
                return;
            }

            // ✅ CORRIGIDO: Aguardar semáforo ANTES de enfileirar
            if (!TryEnqueueWithBackpressure(entry))
            {
                return;
            }

            if (_queue.Count >= _options.BatchOptions.BatchSize)
            {
                Flush();
            }
        }

        public async Task EnqueueAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            if (_disposed || entry == null)
                return;

            if (!_options.UseGlobalBuffer)
            {
                if (_sink is IAsyncLogSink asyncSink)
                {
                    await asyncSink.WriteAsync(entry, cancellationToken);
                }
                else
                {
                    _sink.Write(entry);  // ✅ Fallback síncrono
                }
                return;
            }

            // ✅ Aplicar backpressure assíncrono
            if (!await TryEnqueueWithBackpressureAsync(entry, cancellationToken))
            {
                return;  // Log descartado
            }

            //_queue.Enqueue(entry);

            if (_queue.Count >= _options.BatchOptions.BatchSize)
            {
                await FlushAsync(cancellationToken);
            }
        }

        // ✅ NOVO: Backpressure síncrono
        private bool TryEnqueueWithBackpressure(ILogEntry entry)
        {
            switch (_options.BackpressureOptions.OverflowStrategy)
            {
                case OverflowStrategy.DropOldest:
                    lock (_flushLock)
                    {
                        while (_queue.Count >= _options.BackpressureOptions.MaxQueueCapacity)
                        {
                            if (_queue.TryDequeue(out _))
                            {
                                Interlocked.Increment(ref _droppedLogsCount);
                            }
                        }
                        _queue.Enqueue(entry);
                    }
                    return true;

                case OverflowStrategy.DropNewest:
                    // ✅ CORRIGIDO: Lock para checagem + enqueue atômico
                    lock (_flushLock)
                    {
                        if (_queue.Count >= _options.BackpressureOptions.MaxQueueCapacity)
                        {
                            Interlocked.Increment(ref _droppedLogsCount);
                            return false;
                        }
                        _queue.Enqueue(entry);
                    }
                    return true;

                case OverflowStrategy.Block:
                    _blockingSemaphore.Wait();
                    lock (_flushLock)
                    {
                        _queue.Enqueue(entry);
                    }
                    return true;

                default:
                    _queue.Enqueue(entry);
                    return true;
            }
        }
        // ✅ NOVO: Backpressure assíncrono
        private async Task<bool> TryEnqueueWithBackpressureAsync(ILogEntry entry, CancellationToken cancellationToken)
        {
            switch (_options.BackpressureOptions.OverflowStrategy)
            {
                case OverflowStrategy.DropOldest:
                    lock (_flushLock)
                    {
                        while (_queue.Count >= _options.BackpressureOptions.MaxQueueCapacity)
                        {
                            if (_queue.TryDequeue(out _))
                            {
                                Interlocked.Increment(ref _droppedLogsCount);
                            }
                        }
                        _queue.Enqueue(entry);
                    }
                    return true;

                case OverflowStrategy.DropNewest:
                    lock (_flushLock)
                    {
                        if (_queue.Count >= _options.BackpressureOptions.MaxQueueCapacity)
                        {
                            Interlocked.Increment(ref _droppedLogsCount);
                            return false;
                        }
                        _queue.Enqueue(entry);
                    }
                    return true;

                case OverflowStrategy.Block:
                    await _blockingSemaphore.WaitAsync(cancellationToken);
                    lock (_flushLock)
                    {
                        _queue.Enqueue(entry);
                    }
                    return true;

                default:
                    _queue.Enqueue(entry);
                    return true;
            }
        }

        public void Flush()
        {
            if (_disposed)
                return;

            lock (_flushLock)
            {
                if (_queue.IsEmpty)
                    return;

                var batch = new List<ILogEntry>();
                while (_queue.TryDequeue(out var entry) && batch.Count < _options.BatchOptions.BatchSize)
                {
                    batch.Add(entry);
                }

                if (batch.Count == 0)
                    return;

                // ✅ CORRIGIDO: Liberar semáforo ANTES de escrever
                if (_blockingSemaphore != null)
                {
                    _blockingSemaphore.Release(batch.Count);
                }

                if (_sink is IBatchLogSink batchSink)
                {
                    try { batchSink.WriteBatch(batch); }
                    catch { }
                }
                else
                {
                    foreach (var entry in batch)
                    {
                        try { _sink.Write(entry); }
                        catch { }
                    }
                }
            }
        }        // ✅ NOVO: Flush assíncrono
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;

            await Task.Run(() =>
            {
                lock (_flushLock)
                {
                    if (_queue.IsEmpty)
                        return;

                    var batch = new List<ILogEntry>();
                    while (_queue.TryDequeue(out var entry) && batch.Count < _options.BatchOptions.BatchSize)
                    {
                        batch.Add(entry);
                    }

                    if (batch.Count == 0)
                        return;

                    // ✅ Libera semáforo
                    if (_blockingSemaphore != null)
                    {
                        _blockingSemaphore.Release(batch.Count);
                    }

                    if (_sink is IAsyncBatchLogSink asyncBatchSink)
                    {
                        asyncBatchSink.WriteBatchAsync(batch, cancellationToken).GetAwaiter().GetResult();
                    }
                    else if (_sink is IBatchLogSink batchSink)
                    {
                        batchSink.WriteBatch(batch);
                    }
                    else
                    {
                        foreach (var entry in batch)
                        {
                            if (_sink is IAsyncLogSink asyncSink)
                            {
                                asyncSink.WriteAsync(entry, cancellationToken).GetAwaiter().GetResult();
                            }
                            else
                            {
                                _sink.Write(entry);
                            }
                        }
                    }
                }
            }, cancellationToken);
        }

        // ✅ NOVO: Métrica de logs descartados
        public long GetDroppedLogsCount() => Interlocked.Read(ref _droppedLogsCount);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _flushTimer?.Dispose();
            Flush();

            _blockingSemaphore?.Dispose();
        }
    }
}
