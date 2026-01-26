using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Sinks
{
    public class FallbackLogSink : ILogSink
    {
        public void WriteFallback(
    BufferedLogEntry original,
    Exception sinkException)
        {
            try
            {
                Console.Error.WriteLine(
                    $"[LOGGING-FAILURE] {sinkException.Message}");
                Console.Error.WriteLine(
                    $"[ORIGINAL] {original.Message}");
            }
            catch
            {
                // Última linha de defesa: silêncio absoluto
            }
        }

        public void Write(ILogEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
