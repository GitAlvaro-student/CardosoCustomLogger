using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Sinks
{
    /// <summary>
    /// Sink simples para escrita de logs no console.
    /// Usado apenas para validação do pipeline.
    /// </summary>
    public sealed class ConsoleLogSink : IBatchLogSink, IDisposable
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
