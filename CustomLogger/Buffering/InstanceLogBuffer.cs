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
        private bool _disposed;

        public InstanceLogBuffer(ILogSink sink, CustomProviderOptions options)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _options = options ?? throw new ArgumentNullException(nameof(options));

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

            _queue.Enqueue(entry);

            // ✅ Flush por tamanho (batch size)
            if (_queue.Count >= _options.BatchOptions.BatchSize)
            {
                Flush();
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
                while (_queue.TryDequeue(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count == 0)
                    return;

                try
                {
                    if (_sink is IBatchLogSink batchSink)
                    {
                        batchSink.WriteBatch(batch);
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
                catch
                {
                    // ✅ Fallback: tenta escrita individual se batch falhou
                    foreach (var entry in batch)
                    {
                        try { _sink.Write(entry); }
                        catch { }
                    }
                }
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

            _queue.Enqueue(entry);

            if (_queue.Count >= _options.BatchOptions.BatchSize)
            {
                await FlushAsync(cancellationToken);
            }
        }

        // ✅ NOVO: Flush assíncrono
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;

            await Task.Run(async () =>
            {
                lock (_flushLock)
                {
                    if (_queue.IsEmpty)
                        return;
                }

                List<ILogEntry> batch;
                lock (_flushLock)
                {
                    batch = new List<ILogEntry>();
                    while (_queue.TryDequeue(out var entry))
                    {
                        batch.Add(entry);
                    }
                }

                if (batch.Count == 0)
                    return;

                try
                {
                    if (_sink is IAsyncBatchLogSink asyncBatchSink)
                    {
                        await asyncBatchSink.WriteBatchAsync(batch, cancellationToken);
                    }
                    else if (_sink is IBatchLogSink batchSink)
                    {
                        batchSink.WriteBatch(batch);
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
                catch
                {
                    // ✅ Fallback: tenta escrita individual se batch falhou
                    foreach (var entry in batch)
                    {
                        try
                        {
                            if (_sink is IAsyncLogSink asyncSink)
                            {
                                await asyncSink.WriteAsync(entry, cancellationToken);
                            }
                            else
                            {
                                _sink.Write(entry);
                            }
                        }
                        catch { }
                    }
                }
            }, cancellationToken);
        }
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Console.WriteLine($"[DEBUG] Dispose - Itens na fila: {_queue.Count}");

            _flushTimer?.Dispose();

            Console.WriteLine("[DEBUG] Dispose - Chamando Flush final");
            Flush();

            Console.WriteLine("[DEBUG] Dispose - Concluído");
        }
    }
}
