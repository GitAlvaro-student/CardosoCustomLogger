using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Sinks
{
    public sealed class FallbackLogSink : ILogSink
    {
        public void Write(ILogEntry entry)
        {
            try
            {
                Console.Error.WriteLine($"[FALLBACK] {entry?.Timestamp} {entry?.LogLevel} {entry?.Message}");
            }
            catch
            {
                // Silêncio absoluto - última linha de defesa
            }
        }
    }
}
