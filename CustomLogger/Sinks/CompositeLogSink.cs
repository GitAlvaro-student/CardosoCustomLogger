using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    public sealed class CompositeLogSink : ILogSink
    {
        private readonly IReadOnlyCollection<ILogSink> _sinks;

        public CompositeLogSink(IEnumerable<ILogSink> sinks)
        {
            _sinks = new List<ILogSink>(sinks);
            Debug.WriteLine($"Total de Sinks: {_sinks.Count}");
        }

        public void Write(ILogEntry entry)
        {
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.Write(entry);
                }
                catch
                {
                    // Falha em um sink não derruba os outros
                }
            }
        }

        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            var batch = entries.ToList();

            foreach (var sink in _sinks)
            {
                try
                {
                    if (sink is IBatchLogSink batchSink)
                    {
                        batchSink.WriteBatch(batch);
                    }
                    else
                    {
                        // ✅ Fallback: escrita individual se sink não suporta batch
                        foreach (var entry in batch)
                        {
                            sink.Write(entry);
                        }
                    }
                }
                catch
                {
                    // Absorve falha - próximo sink continua
                }
            }
        }

        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            foreach (var sink in _sinks)
            {
                try
                {
                    if (sink is IAsyncLogSink asyncSink)
                    {
                        await asyncSink.WriteAsync(entry, cancellationToken);
                    }
                    else
                    {
                        // ✅ Fallback: escrita síncrona se sink não suporta async
                        sink.Write(entry);
                    }
                }
                catch
                {
                    // Absorve falha - próximo sink continua
                }
            }
        }

        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            var batch = entries.ToList();

            foreach (var sink in _sinks)
            {
                try
                {
                    if (sink is IAsyncBatchLogSink asyncBatchSink)
                    {
                        await asyncBatchSink.WriteBatchAsync(batch, cancellationToken);
                    }
                    else if (sink is IBatchLogSink batchSink)
                    {
                        // ✅ Fallback: batch síncrono
                        batchSink.WriteBatch(batch);
                    }
                    else
                    {
                        // ✅ Fallback: escrita individual síncrona
                        foreach (var entry in batch)
                        {
                            sink.Write(entry);
                        }
                    }
                }
                catch
                {
                    // Absorve falha - próximo sink continua
                }
            }
        }
    }
}
