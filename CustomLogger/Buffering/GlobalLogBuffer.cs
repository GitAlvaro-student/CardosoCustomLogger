using CustomLogger.Abstractions;
using CustomLogger.Configurations;
using CustomLogger.Formatting;
using CustomLogger.Sinks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Buffering
{
    /// <summary>
    /// Buffer global de logs compartilhado por todos os loggers.
    /// </summary>
    public static class GlobalLogBuffer
    {
        private static ILogSink _sink;
        private static readonly ConcurrentQueue<BufferedLogEntry> _queue =
            new ConcurrentQueue<BufferedLogEntry>();

        public static void Configure(ILogSink sink)
        {
            _sink = sink;

        }
        /// <summary>
        /// Adiciona um log ao buffer ou escreve imediatamente,
        /// dependendo da configuração.
        /// </summary>
        public static void Enqueue(
            BufferedLogEntry entry,
            CustomProviderConfiguration configuration)
        {
            if (entry == null || configuration == null)
                return;

            if (!configuration.Options.UseGlobalBuffer)
            {
                Write(entry);
                return;
            }

            _queue.Enqueue(entry);

            if (_queue.Count >= configuration.Options.MaxBufferSize)
            {
                Flush();
            }
        }

        /// <summary>
        /// Descarrega todo o buffer.
        /// </summary>
        public static void Flush()
        {
            while (_queue.TryDequeue(out var entry))
            {
                Write(entry);
            }
        }

        /// <summary>
        /// Escrita final do log.
        /// Neste estágio, é apenas um placeholder.
        /// </summary>
        private static void Write(BufferedLogEntry entry)
        {
            _sink?.Write(entry);
        }
    }
}
