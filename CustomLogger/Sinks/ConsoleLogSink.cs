using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    /// <summary>
    /// Sink simples para escrita de logs no console.
    /// Usado apenas para validação do pipeline.
    /// </summary>
    public sealed class ConsoleLogSink : IAsyncBatchLogSink, IDisposable
    {
        private readonly ILogFormatter _formatter;

        public ConsoleLogSink(ILogFormatter formatter)
        {
            _formatter = formatter;
        }

        public void Write(ILogEntry entry)
        {
            if (entry == null)
                return;

            try
            {
                Console.WriteLine(_formatter.Format(entry));
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: Escrita em lote
        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            if (entries == null)
                return;

            try
            {
                foreach (var entry in entries)
                {
                    Console.WriteLine(_formatter.Format(entry));
                }
                Console.Out.Flush();  // ✅ Flush UMA VEZ
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: Write assíncrono
        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null)
                return;

            try
            {
                await Console.Out.WriteLineAsync(_formatter.Format(entry));
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: WriteBatch assíncrono
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (entries == null)
                return;

            try
            {
                foreach (var entry in entries)
                {
                    await Console.Out.WriteLineAsync(_formatter.Format(entry));
                }
                await Console.Out.FlushAsync();
            }
            catch
            {
                // Absorve falha
            }
        }

        public void Dispose()
        {
            try
            {
                Console.Out.Flush();
            }
            catch
            {
                // Absorve falha
            }
        }
    }
}
