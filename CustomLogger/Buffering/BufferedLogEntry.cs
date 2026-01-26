using CustomLogger.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Buffering
{
    /// <summary>
    /// Representa um evento de log estruturado armazenado no buffer.
    /// </summary>
    public sealed class BufferedLogEntry : ILogEntry
    {
        public DateTimeOffset Timestamp { get; set; }

        public string Category { get; set; }

        public LogLevel LogLevel { get; set; }

        public EventId EventId { get; set; }

        public string Message { get; set; }

        public Exception Exception { get; set; }

        public object State { get; set; }

        public IReadOnlyDictionary<string, object> Scopes { get; set; }

    }
}
